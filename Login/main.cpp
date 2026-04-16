#include "pch.h"
#include "portal_login.h"
#include <filesystem>

namespace fs = std::filesystem;
using json = nlohmann::json;

static std::string GetExeDirectory() {
    char buffer[MAX_PATH];
    GetModuleFileNameA(nullptr, buffer, MAX_PATH);
    
    std::string::size_type pos = std::string(buffer).find_last_of("\\/");
    return std::string(buffer).substr(0, pos);
}

static bool LoadConfig(std::string& username, std::string& password) {
    std::string configPath = GetExeDirectory() + "\\config.json";
    
    if (!fs::exists(configPath)) {
        std::cerr << "[错误] 配置文件不存在: " << configPath << std::endl;
        std::cerr << "请创建 config.json 文件，格式如下：" << std::endl;
        std::cerr << "{\n    \"username\": \"你的学号\",\n    \"password\": \"你的密码\"\n}" << std::endl;
        return false;
    }
    
    try {
        std::ifstream file(configPath);
        if (!file.is_open()) {
            std::cerr << "[错误] 无法打开配置文件: " << configPath << std::endl;
            return false;
        }
        
        json config = json::parse(file);
        file.close();
        
        if (config.contains("username") && config.contains("password")) {
            username = config["username"];
            password = config["password"];
            return true;
        } else {
            std::cerr << "[错误] 配置文件格式错误，缺少 username 或 password 字段" << std::endl;
            return false;
        }
    }
    catch (const std::exception& e) {
        std::cerr << "[错误] 解析配置文件失败: " << e.what() << std::endl;
        return false;
    }
}

int main() {
    std::cout << "========================================" << std::endl;
    std::cout << "       AutoPortal 自动登录程序" << std::endl;
    std::cout << "========================================" << std::endl;
    
    std::string username, password;
    if (!LoadConfig(username, password)) {
        std::cout << "\n按任意键退出..." << std::endl;
        std::cin.get();
        return 1;
    }
    
    std::cout << "\n[信息] 学号: " << username << std::endl;
    std::cout << "[信息] 正在登录..." << std::endl;
    
    curl_global_init(CURL_GLOBAL_DEFAULT);
    
    std::string portal_url = "http://10.189.108.11/";
    
    PortalLogin login(portal_url);
    std::string result = login.login(username, password, true);
    
    curl_global_cleanup();
    
    std::cout << "\n========================================" << std::endl;
    if (result.find("success:") == 0) {
        std::cout << "[登录成功] " << result.substr(8) << std::endl;
    } else {
        std::cout << "[登录失败] " << result.substr(6) << std::endl;
    }
    std::cout << "========================================" << std::endl;
    
    std::cout << "\n按任意键退出..." << std::endl;
    std::cin.get();
    return 0;
}
