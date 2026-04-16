// ai封装后
#pragma once

#include <string>
#include <vector>
#include <sstream>
#include <iomanip>
#include <chrono>
#include <curl/curl.h>

// 静态回调函数，用于接收 HTTP 响应
static size_t WriteCallback(void* contents, size_t size, size_t nmemb, std::string* response) {
    size_t total = size * nmemb;
    response->append((char*)contents, total);
    return total;
}

class PortalLogin {
public:
    /**
     * 构造函数
     * @param portal_url Portal 页面完整 URL（例如 http://10.189.108.11/ac_portal/...）
     */
    explicit PortalLogin(const std::string& portal_url);

    /**
     * 执行登录
     * @param username     用户名
     * @param password     密码
     * @param remember_pwd 是否记住密码（传 true）
     * @return 登录结果字符串，以 "success:" 或 "error:" 开头
     */
    std::string login(const std::string& username, const std::string& password, bool remember_pwd = true);

private:
    std::string base_url_;       // 基础 URL（例如 http://10.189.108.11）
    std::string portal_url_;     // 原始 portal 页面 URL

    // RC4 加密（与 Python/JS 版本兼容）
    static std::string rc4_encrypt(const std::string& src, const std::string& passwd);
    // 生成毫秒时间戳作为密钥
    static std::string generate_timestamp_key();
    // 发送 POST 请求
    bool http_post(const std::string& url, const std::string& data,
        const std::vector<std::string>& headers,
        std::string& response, long& http_code);
    // 判断响应是否表示成功
    static bool looks_like_success(const std::string& text);
    // 获取可能的登录接口地址列表
    std::vector<std::string> get_login_url_candidates() const;
};

