// testcase.hpp: JniGen test case header file. Doesn't serve any actual purpose.

// The export macros MUST be named dllx in order to get recognized.

#define dllx extern "C" __declspec(dllexport)

// Functions go below:

dllx double jnigen_test(double val);
dllx const char* jnigen_teststring(char* v, double a);
dllx double jnigen_star(char *abc);

