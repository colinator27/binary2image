# binary2image
A program that converts (binary) files to PNG images, and vice versa. It can generate some interesting visuals.

Just simply run the program. By default it uses a file called `test.txt` and generates `test.png`, then converts it back to `test-back.txt`.
If all goes well, the contents of `test.txt` and `test-back.txt` should be the same.

To change the files simply change (as of initial code commit) lines 18 and 19 in `Program.cs`.

***Notice: This program does not support differing endianness, and therefore it is possible that some files generated will not be compatible with certain computers.***
