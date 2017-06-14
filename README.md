# AES Encryption [v0.0.1]
This a console command line file encrypter using [Advanced Encryption Standard](https://en.wikipedia.org/wiki/Advanced_Encryption_Standard)

## How to use
- Creating a custom key: --create --key your_key
- Creating 128/256/512 Bit key: --create --key128/256/512
- Encrypting a folder: --encrypt C:\folder\path
- Decrypting a folder: --decrypt C:\folder\path

## version 0.0.1
- Create your own key
- Generate a random key of 16, 32, 64 bytes
- Encrypt files in a folder
- AES KeySize is fixed (256 Bit)
- AES BlockSize is fixed (256 Bit)
- Cipher Mode is set to [Cipher Feedback](https://en.wikipedia.org/wiki/Block_cipher_mode_of_operation#Cipher_Feedback_.28CFB.29)
- Decrypt files in a folder
