﻿@echo
@C:
@cd \Users\Stefan\Documents\A A A C_Compiler_Assembler - CSharp\C_Compiler_CSharp\C_Compiler_CSharp

@"C:\gppg-distro-1_5_2\binaries\Gppg" /gplex MainParser.gppg > MainParser.cs
@"C:\gppg-distro-1_5_2\binaries\Gplex" MainScanner.gplex

@"C:\gppg-distro-1_5_2\binaries\Gppg" /gplex PreParser.gppg > PreParser.cs
@"C:\gppg-distro-1_5_2\binaries\Gplex" PreScanner.gplex

@"C:\gppg-distro-1_5_2\binaries\Gppg" /gplex ExpressionParser.gppg > ExpressionParser.cs
@"C:\gppg-distro-1_5_2\binaries\Gplex" ExpressionScanner.gplex

@pause