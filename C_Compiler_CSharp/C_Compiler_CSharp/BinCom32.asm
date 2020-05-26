            bits 64
  	  org 100h

;	  mov dx,hello
	  mov ah,9
	  int 0x21

	  mov rax,0x4c00
	  int 0x21

;hello:	  db 'Hello World$'

