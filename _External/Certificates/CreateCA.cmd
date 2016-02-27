"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\makecert.exe" ^
-n "CN=TwithTally Certificate Authority" ^
-r ^
-pe ^
-a sha256 ^
-len 2048 ^
-cy authority ^
-sv TwithTallyCA.pvk ^
TwithTallyCA.cer

"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\pvk2pfx.exe" ^
-pvk TwithTallyCA.pvk ^
-spc TwithTallyCA.cer ^
-pfx TwithTallyCA.pfx ^
-po TwithTallyCA