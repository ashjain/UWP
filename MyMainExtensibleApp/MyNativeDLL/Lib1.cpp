#include <string>
#include <cstdint>

class Lib1
{
	public:
		std::string GetString()
		{
			return "Native C++ DLL function inside a UWP app Extension has been called successfully - Hello World!";
		}
};

// DLL API
#if defined(LIB1_EXPORTS)
#define LIB1_API __declspec(dllexport)
#else
#define LIB1_API __declspec(dllimport)
#endif

extern "C"
{
	LIB1_API uint32_t GetString(char * buffer, uint32_t length)
	{
		Lib1 lib1;
		auto val = snprintf(buffer, length, lib1.GetString().c_str());

		return 0;
	}
}