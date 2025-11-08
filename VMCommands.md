Virtual Machine Command Reference
===================================

Addressing Mode Notation:
- M = Memory index (result destination)
- O1 = First operand index
- O2 = Second operand index
- direct = Direct addressing (absolute memory index)
- indirect = Indirect addressing (relative to frame pointer)

ASSIGNMENT COMMANDS
===================

Direct Assignment:
------------------
Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
iass    | M, O1    | Integer assignment   | Mem[M] = Mem[O1]
rass    | M, O1    | Real assignment      | Mem[M] = Mem[O1]
bass    | M, O1    | Boolean assignment   | Mem[M] = Mem[O1]

Constant Assignment:
-------------------
Command | Operands   | Description          | Semantics
--------|------------|----------------------|-----------
icass   | M, const   | Integer constant     | Mem[M] = const_i
rcass   | M, const   | Real constant        | Mem[M] = const_r
bcass   | M, const   | Boolean constant     | Mem[M] = const_b

Assignment with Operation:
--------------------------
Command   | Operands    | Description               | Semantics
----------|-------------|---------------------------|-----------
iassadd   | M, O1, O2  | Integer add and assign    | Mem[M] = Mem[O1] + Mem[O2]
rassadd   | M, O1, O2  | Real add and assign       | Mem[M] = Mem[O1] + Mem[O2]
iasssub   | M, O1, O2  | Integer subtract and assign | Mem[M] = Mem[O1] - Mem[O2]
rasssub   | M, O1, O2  | Real subtract and assign  | Mem[M] = Mem[O1] - Mem[O2]

ARITHMETIC OPERATIONS
=====================

Integer Arithmetic:
-------------------
Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
iadd    | M, O1, O2 | Integer addition   | Mem[M] = Mem[O1] + Mem[O2]
isub    | M, O1, O2 | Integer subtraction | Mem[M] = Mem[O1] - Mem[O2]
imul    | M, O1, O2 | Integer multiplication | Mem[M] = Mem[O1] * Mem[O2]
idiv    | M, O1, O2 | Integer division    | Mem[M] = Mem[O1] / Mem[O2]

Real Arithmetic:
----------------
Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
radd    | M, O1, O2 | Real addition      | Mem[M] = Mem[O1] + Mem[O2]
rsub    | M, O1, O2 | Real subtraction   | Mem[M] = Mem[O1] - Mem[O2]
rmul    | M, O1, O2 | Real multiplication | Mem[M] = Mem[O1] * Mem[O2]
rdiv    | M, O1, O2 | Real division      | Mem[M] = Mem[O1] / Mem[O2]

COMPARISON OPERATIONS
=====================

Integer Comparisons:
--------------------
Command | Operands   | Description          | Semantics
--------|------------|----------------------|-----------
ilt     | M, O1, O2 | Integer less than    | Mem[M] = Mem[O1] < Mem[O2]
igt     | M, O1, O2 | Integer greater than | Mem[M] = Mem[O1] > Mem[O2]
ieq     | M, O1, O2 | Integer equality     | Mem[M] = Mem[O1] == Mem[O2]
ineq    | M, O1, O2 | Integer inequality   | Mem[M] = Mem[O1] != Mem[O2]
ic2ge   | M, O1, const | Int compare to const ? | Mem[M] = Mem[O1] >= const_i
ic2le   | M, O1, const | Int compare to const ? | Mem[M] = Mem[O1] <= const_i

Real Comparisons:
-----------------
Command | Operands   | Description          | Semantics
--------|------------|----------------------|-----------
rlt     | M, O1, O2 | Real less than       | Mem[M] = Mem[O1] < Mem[O2]
rgt     | M, O1, O2 | Real greater than    | Mem[M] = Mem[O1] > Mem[O2]
req     | M, O1, O2 | Real equality        | Mem[M] = abs(Mem[O1] - Mem[O2]) < ?
rneq    | M, O1, O2 | Real inequality      | Mem[M] = abs(Mem[O1] - Mem[O2]) >= ?
rc2ge   | M, O1, const | Real compare to const ? | Mem[M] = Mem[O1] >= const_r
rc2le   | M, O1, const | Real compare to const ? | Mem[M] = Mem[O1] <= const_r

Boolean Comparisons:
--------------------
Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
beq     | M, O1, O2 | Boolean equality   | Mem[M] = Mem[O1] == Mem[O2]
bneq    | M, O1, O2 | Boolean inequality | Mem[M] = Mem[O1] != Mem[O2]

TYPE CONVERSION
===============

Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
citr    | M, O1    | Convert int to real | Mem[O1] = (double)Mem[M]

CONTROL FLOW
============

Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
iif     | O1, label | Conditional jump if true | if Mem[O1] then PC = label
ifn     | O1, label | Conditional jump if false | if !Mem[O1] then PC = label
go      | label    | Unconditional jump   | PC = label
label   | label    | Label marker        | (no operation)
stop    | -        | Stop execution      | Halt VM

FUNCTION CALLS
==============

Command | Operands | Description          | Semantics
--------|----------|----------------------|-----------
call    | M, label | Function call       | Push return address; PC = label; Save result to M
param   | O1       | Parameter passing   | Push parameter onto param stack
push    | O1       | Push to stack       | Push Mem[O1] onto stack
pop     | M        | Pop from stack      | Mem[M] = Pop from stack
creturn | -        | Return from function | PC = Pop return address; Restore frame

ADDRESSING MODE SUFFIXES
========================

Single Operand Indirect:
------------------------
Suffix | Affected Operands | Description
-------|-------------------|------------
_l     | O1 indirect       | Left operand uses frame-relative addressing
_r     | O2 indirect       | Right operand uses frame-relative addressing
_d     | M indirect        | Result uses frame-relative addressing

Dual Operand Indirect:
----------------------
Suffix | Affected Operands | Description
-------|-------------------|------------
_ld    | O1, M indirect    | Left operand and result use frame addressing
_rd    | O2, M indirect    | Right operand and result use frame addressing
_lr    | O1, O2 indirect   | Both operands use frame addressing

Triple Operand Indirect:
------------------------
Suffix | Affected Operands | Description
-------|-------------------|------------
_lrd   | O1, O2, M indirect | All operands use frame addressing

EXAMPLES
========

Direct Addressing (Global Variables):
-------------------------------------
icass 0, 10       # Mem[0] = 10 (global)
icass 1, 20       # Mem[1] = 20 (global)  
iadd 2, 0, 1      # Mem[2] = Mem[0] + Mem[1]

Indirect Addressing (Local Variables in Function Frame):
--------------------------------------------------------
icass_l 0, 5      # Frame[0] = 5 (local)
icass_l 1, 3      # Frame[1] = 3 (local)
iadd_ld 2, 0, 1   # Frame[2] = Frame[0] + Mem[1] (mixed addressing)

Mixed Addressing:
-----------------
iadd_rd 2, 0, 1   # Mem[2] = Mem[0] + Frame[1] (global result, local right operand)

MEMORY LAYOUT
=============

- Global variables: Direct addressing (absolute indices)
- Local variables: Indirect addressing (frame-relative indices)
- Temporary results: Typically use direct addressing
- Function parameters: Passed via stack operations

NOTES
=====

- ? (epsilon) = 0.00000001 for floating-point comparisons
- Frame pointer (_current_frame_index) points to base of current stack frame
- Indirect addressing: actual_address = _current_frame_index + index
- All memory accesses are bounds-checked with EnsureMemorySize()