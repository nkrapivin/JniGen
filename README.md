# JniGen
Generate bindings for JNI with ease. **Targeted at GameMaker.**

## Limitations
- A function must start with `dllx ` in order to be recognized:
  - this won't work `double func();`
  - this will work `dllx double func();`
- Argument types without variable names are not allowed:
  - this won't work `dllx double func(double, const char*);`
  - this will work `dllx double func(double a, const char* b);`
- The star at the start of the variable name is not allowed:
  - this won't work `dllx char *func();` / `dllx double func(char *a);` / `dllx double func(char * a);`
  - this will work `dllx char* func();` / `dllx double func(char* a);`
- `void` (without the star) is not allowed:
  - this won't work `dllx double func(void);`
  - this will work `dllx double func();`
- If the return type is anything but `double`, the return type will be `java.lang.String`
  - this is because of GameMaker limitations.
- This tool is targeted at header (`.h/.hpp`) files and not implementation files (`.c/.cpp`)
- This tool is more targeted at C++ rather than C. (i.e. `extern "C"` in default output)
