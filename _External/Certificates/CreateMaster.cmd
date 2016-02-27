"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\makecert.exe" ^
-n "CN=TwithTally Master Server" ^
-iv TwithTallyCA.pvk ^
-ic TwithTallyCA.cer ^
-pe ^
-a sha256 ^
-len 2048 ^
-b 02/01/2016 ^
-e 12/31/2039 ^
-sky exchange ^
-eku 1.3.6.1.5.5.7.3.1 ^
-sv TwithTallyMaster.pvk ^
TwithTallyMaster.cer

"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\pvk2pfx.exe" ^
-pvk TwithTallyMaster.pvk ^
-spc TwithTallyMaster.cer ^
-pfx TwithTallyMaster.pfx ^
-po TwithTallyMaster