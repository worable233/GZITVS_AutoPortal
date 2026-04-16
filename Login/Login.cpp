#include "pch.h"
#include "Login.h"
#include <filesystem>

namespace fs = std::filesystem;
using json = nlohmann::json;

// 设置错误信息
static void SetError(wchar_t** errorMsg, const std::wstring& msg) {
    if (errorMsg) {
        *errorMsg = new wchar_t[msg.size() + 1];
        wcscpy_s(*errorMsg, msg.size() + 1, msg.c_str());
    }
}

// 宽字符转窄字符
static std::string ToUTF8(const std::wstring& wstr) {
    if (wstr.empty()) return "";
    int size = WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), -1, nullptr, 0, nullptr, nullptr);
    std::string result(size - 1, 0);
    WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), -1, &result[0], size, nullptr, nullptr);
    return result;
}

// 窄字符转宽字符
static std::wstring ToWide(const std::string& str) {
    if (str.empty()) return L"";
    int size = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, nullptr, 0);
    std::wstring result(size - 1, 0);
    MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, &result[0], size);
    return result;
}

// 获取程序目录
static std::wstring GetExeDir() {
    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(nullptr, buffer, MAX_PATH);
    std::wstring path(buffer);
    return path.substr(0, path.find_last_of(L"\\/"));
}

// 获取配置文件路径
static std::wstring GetConfigPath() {
    return GetExeDir() + L"\\config.json";
}

// 分配字符串
static wchar_t* AllocString(const std::wstring& str) {
    wchar_t* result = new wchar_t[str.size() + 1];
    wcscpy_s(result, str.size() + 1, str.c_str());
    return result;
}

// ========== 导出函数实现 ==========

extern "C" LOGINVALIDATOR_API int __stdcall ValidateLogin(
    const wchar_t* studentId, 
    const wchar_t* password,
    wchar_t** errorMsg
) {
    // 参数检查
    if (!studentId || !password) {
        SetError(errorMsg, L"参数不能为空");
        return 0;
    }

    try {
        // 初始化 libcurl
        curl_global_init(CURL_GLOBAL_DEFAULT);

        // 读取配置获取 portal_url
        std::string portalUrl = "http://10.189.108.11/"; // 默认地址
        
        std::wstring configPath = GetConfigPath();
        if (fs::exists(configPath)) {
            std::ifstream file(configPath);
            if (file.is_open()) {
                try {
                    json config = json::parse(file);
                    if (config.contains("portal_url")) {
                        portalUrl = config["portal_url"];
                    }
                } catch (...) {
                    // 解析失败使用默认地址
                }
                file.close();
            }
        }

        // 执行登录
        PortalLogin login(portalUrl);
        std::string result = login.login(
            ToUTF8(studentId), 
            ToUTF8(password), 
            true
        );

        // 清理 libcurl
        curl_global_cleanup();

        // 判断结果
        if (result.find("success:") == 0) {
            return 1;
        } else {
            SetError(errorMsg, ToWide(result));
            return 0;
        }
    }
    catch (const std::exception& e) {
        SetError(errorMsg, ToWide(std::string("登录异常: ") + e.what()));
        return 0;
    }
}

extern "C" LOGINVALIDATOR_API wchar_t* __stdcall LoadConfig(wchar_t** errorMsg) {
    try {
        std::wstring configPath = GetConfigPath();

        // 文件不存在返回空对象
        if (!fs::exists(configPath)) {
            return AllocString(L"{}");
        }

        // 读取文件
        std::ifstream file(configPath);
        if (!file.is_open()) {
            SetError(errorMsg, L"无法打开配置文件");
            return AllocString(L"{}");
        }

        // 解析 JSON
        json config = json::parse(file);
        file.close();

        return AllocString(ToWide(config.dump()));
    }
    catch (const std::exception& e) {
        SetError(errorMsg, ToWide(std::string("加载配置失败: ") + e.what()));
        return AllocString(L"{}");
    }
}

extern "C" LOGINVALIDATOR_API int __stdcall SaveConfig(
    const wchar_t* jsonString,
    wchar_t** errorMsg
) {
    // 参数检查
    if (!jsonString) {
        SetError(errorMsg, L"配置内容不能为空");
        return 0;
    }

    try {
        // 解析 JSON
        json config = json::parse(ToUTF8(jsonString));

        // 写入文件
        std::wstring configPath = GetConfigPath();
        std::ofstream file(configPath);
        if (!file.is_open()) {
            SetError(errorMsg, L"无法创建配置文件");
            return 0;
        }

        file << config.dump(4);
        file.close();

        return 1;
    }
    catch (const std::exception& e) {
        SetError(errorMsg, ToWide(std::string("保存配置失败: ") + e.what()));
        return 0;
    }
}

extern "C" LOGINVALIDATOR_API int __stdcall DeleteConfig() {
    try {
        std::wstring configPath = GetConfigPath();
        if (fs::exists(configPath)) {
            return fs::remove(configPath) ? 1 : 0;
        }
        return 1;
    }
    catch (...) {
        return 0;
    }
}

extern "C" LOGINVALIDATOR_API void __stdcall FreeString(wchar_t* ptr) {
    delete[] ptr;
}
