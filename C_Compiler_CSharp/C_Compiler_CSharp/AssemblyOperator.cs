namespace CCompiler {
  public enum AssemblyOperator {
    add, add_byte, add_dword, add_qword, add_word, return_address,
    and, and_byte, and_dword, and_qword, and_word, call,
    cmp, cmp_byte, cmp_dword, cmp_qword, cmp_word, comment,
    dec, dec_byte, dec_dword, dec_qword, dec_word,
    define_address, define_value, define_zero_sequence,
    div, div_byte, div_dword, div_qword, div_word, empty,
    //fabs,
    fadd, faddp, fchs, fcompp, fdiv, fdivp, fdivr,
    fdivrp, fild_dword, fild_qword, fild_word,
    fist_dword, fist_qword, fist_word,
    fistp_dword, fistp_qword, fistp_word,
    fld1, fld_dword, fld_qword, fldcw, fldz,
    fmul, fmulp, fst_dword, fst_qword, fstcw,
    fstp_dword, fstp_qword, fstsw,
    fsub, fsubp, fsubr, fsubrp, ftst,
    idiv, idiv_byte, idiv_dword, idiv_qword, idiv_word,
    imul, imul_byte, imul_dword, imul_qword, imul_word,
    inc, inc_byte, inc_dword, inc_qword, inc_word, interrupt,
    ja, jae, jb, jbe, jc, je, jg, jge, jl, jle, jmp, jnc, jne, jnz, jz,
    label, lahf, mov, mov_byte, mov_dword, mov_qword, mov_word,
    mul, mul_byte, mul_dword, mul_qword, mul_word,
    neg, neg_byte, neg_dword, neg_qword, neg_word, new_middle_code,
    nop, not, not_byte, not_dword, not_qword, not_word,
    or, or_byte, or_dword, or_qword, or_word, pop, ret, sahf, set_track_size,
    shl, shl_byte, shl_dword, shl_qword, shl_word,
    shr, shr_byte, shr_dword, shr_qword, shr_word,
    sub, sub_byte, sub_dword, sub_qword, sub_word, syscall,
    xor, xor_byte, xor_dword, xor_qword, xor_word
  };
};

/*namespace CCompiler {
  public enum AssemblyOperator {
    add,
    add_byte,
    add_dword,
    add_qword,
    add_word,
    address_return,
    and,
    and_byte,
    and_dword,
    and_qword,
    and_word,
    call,
    cmp,
    cmp_byte,
    cmp_dword,
    cmp_qword,
    cmp_word,
    comment,
    dec,
    dec_byte,
    dec_dword,
    dec_qword,
    dec_word,
    define_address,
    define_value,
    define_zero_sequence,
    div,
    div_byte,
    div_dword,
    div_qword,
    div_word,
    empty,
    fabs,
    fadd,
    faddp,
    fchs,
    fcompp,
    fdiv,
    fdivp,
    fdivr,
    fdivrp,
    fild_dword,
    fild_qword,
    fild_word,
    fist_dword,
    fist_qword,
    fist_word,
    fistp_dword,
    fistp_qword,
    fistp_word,
    fld1,
    fld_dword,
    fld_qword,
    fldcw,
    fldz,
    fmul,
    fmulp,
    fst_dword,
    fst_qword,
    fstcw,
    fstp_dword,
    fstp_qword,
    fstsw,
    fsub,
    fsubp,
    fsubr,
    fsubrp,
    ftst,
    idiv,
    idiv_byte,
    idiv_dword,
    idiv_qword,
    idiv_word,
    imul,
    imul_byte,
    imul_dword,
    imul_qword,
    imul_word,
    inc,
    inc_byte,
    inc_dword,
    inc_qword,
    inc_word,
    interrupt,
    ja,
    jae,
    jb,
    jbe,
    jc,
    je,
    jg,
    jge,
    jl,
    jle,
    jmp,
    jnc,
    jne,
    jnz,
    jz,
    label,
    lahf,
    mov,
    mov_byte,
    mov_dword,
    mov_qword,
    mov_word,
    mul,
    mul_byte,
    mul_dword,
    mul_qword,
    mul_word,
    neg,
    neg_byte,
    neg_dword,
    neg_qword,
    neg_word,
    new_middle_code,
    nop,
    not,
    not_byte,
    not_dword,
    not_qword,
    not_word,
    or,
    or_byte,
    or_dword,
    or_qword,
    or_word,
    pop,
    ret,
    sahf,
    set_track_size,
    shl,
    shl_byte,
    shl_dword,
    shl_qword,
    shl_word,
    shr,
    shr_byte,
    shr_dword,
    shr_qword,
    shr_word,
    sub,
    sub_byte,
    sub_dword,
    sub_qword,
    sub_word,
    syscall,
    xor,
    xor_byte,
    xor_dword,
    xor_qword,
    xor_word
  };
};*/