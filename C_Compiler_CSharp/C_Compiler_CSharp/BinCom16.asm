            bits 16
  	  org 100h

            mov ah, 0x02
            mov dl, 'X'
	  int 0x21

	  mov ah, 0x4c
	  mov al, 0
	  int 0x21

;	  mov ah, 0x09
;            mov dx, hello
;	  int 0x21

;hello:	  db 'Hello World$'

;char:       db 'X'
