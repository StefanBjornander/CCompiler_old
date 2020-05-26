; PE64smallest.asm   Aug 19, 2018 (c) DrakoPensulo
; A smallest PE32+ executable (x64)
; 
; Features:
;  - Windows Vista/7/8/10 compatible
;  - Size: 268 bytes (an executable file on x64 Windows cannot be smaller)
;  - No sections
;  - No Data Directories (in particular no imports and no TLS callbacks)
;  - Exits with code 0x2a (this executable does nothing else than that)
;
;
; Compile using FASM (https://flatassembler.net)  command line: fasm.exe PE64smallest.asm

format binary as 'exe' 
use64 


EntryPoint:
db 'MZ'     ; DOS signature
dw 0faceh

dd 00004550h    ; Signature PE\0\0
dw 8664h    ; Machine
dw 0000h    ; NumberOfSections

dd 0facefaceh   ; TimeDateStamp

dd 0facefaceh   ; PointerToSymbolTable

dd 0facefaceh   ; NumberOfSymbols

dw 0        ; SizeOfOptionalHeader      ; must be multiple of 8 not too large 
dw 002fh    ; Characteristics       ; must be bit 1=1 bit 13=0

dw 020Bh    ; PE32+ Magic
db 0fah     ; MajorLinkerVersion
db 0fah     ; MinorLinkerVersion

dd 0facefaceh   ; SizeOfCode

dd 0facefaceh   ; SizeOfInitializedData

dd 0facefaceh   ; SizeOfUninitializedData

dd start    ; AddressOfEntryPoint       ; cannot be smaller than SizeOfHeaders

dd 0facefaceh   ; BaseOfCode

dq 0000000100000000h    ; ImageBase     ; must be multiple of 64k

dd 4        ; SectionAlignment and e_lfanew ; PE header offset in file

dd 4        ; FileAlignment

dw 0faceh   ; MajorOperatingSystemVersiom

dw 0faceh   ; MinorOperatingSystemVersion

dw 0faceh   ; MajorImageVersion

dw 0faceh   ; MinorImageVersion

dw 5        ; MajorSubsystemVersion     ; >3.1 or 4  
dw 0h       ; MinorSubsystemVersion

dd 0facefaceh   ; Win32VersionValue     

dd 0400h    ; SizeOfImage           ; MSB has to be small, must be >0200h

dd start    ; SizeOfHeaders         ; SizeOfHeaders has to be < SizeOfImage

dd 0facefaceh   ; CheckSum

dw 0002h    ; Subsystem 2-GUI 3-CUI
dw 0        ; DllCharacteristics

dd 000cefaceh
dd 0        ; SizeOfStackReserve  upper dword has to be 0, MSB of lower dword has to be small

dd 000cefaceh
dd 0        ; SizeOfStackCommit  upper dword has to be 0, MSB of lower dword has to be small

dd 000cefaceh
dd 0        ; SizeOfHeapReserve  upper dword has to be 0, MSB of lower dword has to be small

dd 000cefaceh
dd 0        ; SizeOfHeapCommit  upper dword has to be 0, MSB of lower dword has to be small

dd 0facefaceh   ; LoaderFlags

dd 0        ; NumberofRvaAndSizes   

dd 0facefaceh
dd 0facefaceh   ; Export Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Import Directory Address and Size

dd 0facefaceh   
dd 0facefaceh   ; Resource Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Exception Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Security Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Base Relocation Table Address and Size

    dd 0facefaceh
dd 0facefaceh   ; Debug Directory Address and Size

dd 0facefaceh   
dd 0facefaceh   ; Architecture Specific Data Address and Size

dd 0facefaceh
dd 0facefaceh   ; RVA of GlobalPtr Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; TLS Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Load Configuration Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Bound Import Directory Address and Size

dd 0facefaceh
dd 0facefaceh   ; Import Address Table Address and Size

dd 0facefaceh
dd 0facefaceh   ; Delay Load Import Descriptors Address and Size

dd 0facefaceh
dd 0facefaceh   ; COM runtime Descriptors Address and Size

dd 0facefaceh

start:
push 2ah
pop rax
ret     ; Reserved Descriptor
