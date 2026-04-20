#pragma once

// Export macro:
// Login.vcxproj currently defines LOGIN_EXPORTS.
// Keep compatibility with both macro names.
#if defined(LOGINVALIDATOR_EXPORTS) || defined(LOGIN_EXPORTS)
#define LOGINVALIDATOR_API __declspec(dllexport)
#else
#define LOGINVALIDATOR_API
#endif

// 使用 extern "C" 防止C++名称修饰，方便C#调用
extern "C" {
    /**
     * 验证登录
     * @param studentId 学号
     * @param password 密码
     * @param errorMsg 错误信息输出参数（可为 nullptr）
     * @return 1=成功，0=失败
     */
    LOGINVALIDATOR_API int __stdcall ValidateLogin(
        const wchar_t* studentId, 
        const wchar_t* password,
        wchar_t** errorMsg
    );

    /**
     * 加载配置文件
     * @param errorMsg 错误信息输出参数（可为 nullptr）
     * @return JSON 字符串，需要调用 FreeString 释放
     */
    LOGINVALIDATOR_API wchar_t* __stdcall LoadConfig(wchar_t** errorMsg);

    /**
     * 保存配置文件
     * @param jsonString JSON 字符串
     * @param errorMsg 错误信息输出参数（可为 nullptr）
     * @return 1=成功，0=失败
     */
    LOGINVALIDATOR_API int __stdcall SaveConfig(
        const wchar_t* jsonString,
        wchar_t** errorMsg
    );

    /**
     * 删除配置文件
     * @return 1=成功，0=失败
     */
    LOGINVALIDATOR_API int __stdcall DeleteConfig();

    /**
     * 释放字符串内存
     * @param ptr 要释放的指针
     */
    LOGINVALIDATOR_API void __stdcall FreeString(wchar_t* ptr);
}
