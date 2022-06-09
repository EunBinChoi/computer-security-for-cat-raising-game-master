# Decompiler Tool

저희는 난독화 기술 중 하나로 디컴파일러 방지를 위해 실행파일 (exe)를 암호화했습니다.
해당 파일의 obfuscatorTest.exe는 이미 암호화 되어있는 상태이고, 이를 디컴파일러 도구로 다시 컴파일 하면 파일이 암호화되어있다는 것을 확인할 수 있습니다.
테스트를 위해 디컴파일러 도구도 함께 첨부하겠습니다.

디컴파일러 도구 사용 방법 : http://www.backerstreet.com/rec/en/how_to_use.html
Use File > New Project... to load an executable file. Executable files can be Windows PE files (.EXE, .DLL), 
Linux ELF files, Mac OS X MachO files or raw files. The level of support for each format varies, with Windows PE having the best support.
 
## Execution Order of Decompiler Tool
1) 압축풀고 BIN폴더 안에 있는 실행파일을 실행합니다. 
2) File> New Project로 실행파일을 엽니다. 
3) 실행파일이라 함은 윈도우는 EXE DLL이 되겠고 리눅스는 ELF 그리고 Mac OS X MachO files 또는 raw 파일들이 되겠습니다. 
4) 하지만 REC는 Windows 실행파일을 가장 잘 지원해줍니다.
5) Decompile> Save to File눌러보시면 REC라는 확장명으로 저장되는데, 메모장으로 열어보면 C code형태로 저장이 되어있습니다.
(이런 과정을 통해 만든 파일이 save.rec 입니다. 디컴파일러 도구가 중간에 실행이 중단되어 중간과정까지만 디컴파일러를 할 수 있었습니다.
하지만 소스 코드가 암호화되어 구조를 파악할 수 조차 없었습니다.)
