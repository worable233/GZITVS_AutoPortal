#include "pch.h"
#include "portal_login.h"
#include <algorithm>
#include <cctype>
#include <iostream>

PortalLogin::PortalLogin(const std::string& portal_url) : portal_url_(portal_url) {
    size_t pos = portal_url_.find("://");
    if (pos != std::string::npos) {
        size_t start = pos + 3;
        size_t end = portal_url_.find('/', start);
        if (end != std::string::npos) {
            base_url_ = portal_url_.substr(0, end);
        }
        else {
            base_url_ = portal_url_;
        }
    }
    else {
        base_url_ = portal_url_;
    }
}

std::string PortalLogin::rc4_encrypt(const std::string& src, const std::string& passwd) {
    std::string clean_src = src;
    size_t start = clean_src.find_first_not_of(" \t\n\r\f\v");
    size_t end = clean_src.find_last_not_of(" \t\n\r\f\v");
    if (start != std::string::npos && end != std::string::npos) {
        clean_src = clean_src.substr(start, end - start + 1);
    }

    size_t plen = passwd.size();
    std::vector<int> key(256), sbox(256);
    for (int i = 0; i < 256; ++i) {
        key[i] = static_cast<int>(passwd[i % plen]);
        sbox[i] = i;
    }

    int j = 0;
    for (int i = 0; i < 256; ++i) {
        j = (j + sbox[i] + key[i]) % 256;
        std::swap(sbox[i], sbox[j]);
    }

    std::ostringstream result;
    int a = 0, b = 0;
    for (size_t i = 0; i < clean_src.size(); ++i) {
        a = (a + 1) % 256;
        b = (b + sbox[a]) % 256;
        std::swap(sbox[a], sbox[b]);
        int c = (sbox[a] + sbox[b]) % 256;
        int encrypted = static_cast<int>(clean_src[i]) ^ sbox[c];
        result << std::hex << std::setw(2) << std::setfill('0') << encrypted;
    }
    return result.str();
}

std::string PortalLogin::generate_timestamp_key() {
    auto now = std::chrono::system_clock::now();
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();
    return std::to_string(ms);
}

bool PortalLogin::http_post(const std::string& url, const std::string& data,
    const std::vector<std::string>& headers,
    std::string& response, long& http_code) {
    CURL* curl = curl_easy_init();
    if (!curl) return false;

    struct curl_slist* header_list = nullptr;
    for (const auto& h : headers) {
        header_list = curl_slist_append(header_list, h.c_str());
    }

    curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
    curl_easy_setopt(curl, CURLOPT_POSTFIELDS, data.c_str());
    curl_easy_setopt(curl, CURLOPT_HTTPHEADER, header_list);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT, 10L);
    curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION, 0L);

    CURLcode res = curl_easy_perform(curl);
    curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &http_code);

    curl_slist_free_all(header_list);
    curl_easy_cleanup(curl);
    return (res == CURLE_OK);
}

bool PortalLogin::looks_like_success(const std::string& text) {
    std::string lower = text;
    std::transform(lower.begin(), lower.end(), lower.begin(), ::tolower);
    return lower.find("\"success\":true") != std::string::npos ||
        lower.find("'success':true") != std::string::npos ||
        lower.find("\"action\":\"location\"") != std::string::npos ||
        lower.find("type=logout") != std::string::npos ||
        lower.find("登录成功") != std::string::npos;
}

std::vector<std::string> PortalLogin::get_login_url_candidates() const {
    return { base_url_ + "/ac_portal/login.php", base_url_ + "/login.php" };
}

std::string PortalLogin::login(const std::string& username, const std::string& password, bool remember_pwd) {
    std::string rckey = generate_timestamp_key();
    std::string encrypted_pwd = rc4_encrypt(password, rckey);

    std::ostringstream params;
    params << "opr=pwdLogin"
        << "&userName=" << username
        << "&pwd=" << encrypted_pwd
        << "&auth_tag=" << rckey
        << "&rememberPwd=" << (remember_pwd ? "1" : "0");

    std::vector<std::string> headers = {
        "Content-Type: application/x-www-form-urlencoded; charset=UTF-8",
        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Referer: " + portal_url_,
        "Origin: " + base_url_,
        "X-Requested-With: XMLHttpRequest"
    };

    for (const auto& login_url : get_login_url_candidates()) {
        std::string response;
        long http_code = 0;
        if (http_post(login_url, params.str(), headers, response, http_code)) {
            if (http_code == 404) continue;

            if (http_code == 302 || http_code == 303)
                return "success: 收到重定向响应";

            if (http_code >= 400)
                return "error: HTTP " + std::to_string(http_code);

            if (looks_like_success(response))
                return "success: 登录成功";

            return "error: 无法识别登录响应";
        }
    }
    return "error: 所有候选登录接口均请求失败";
}
