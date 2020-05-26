  	org 100h

	  mov dx,hello
	  mov ah,9
	  int 0x21

	  mov ax,0x4c00
	  int 0x21

hello:	  db 'hello, world', 13, 10, '$'

