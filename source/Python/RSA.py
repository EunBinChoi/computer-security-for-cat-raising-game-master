from Crypto.PublicKey import RSA
from Crypto import Random

key = RSA.generate(2048)

file_py_priv = open('pyPrivKey.pem','r')
file_py_pub = open('pyPubKey.pem','r')
py_pub_key = RSA.importKey(file_py_pub.read())
py_priv_key = RSA.importKey(file_py_priv.read())

file_uni_priv = open('unPrivKey.pem','r')
file_uni_pub = open('unPubKey.pem','r')
uni_pub_key = RSA.importKey(file_uni_pub.read())
uni_priv_key = RSA.importKey(file_uni_priv.read())

file_py_priv.close()
file_py_pub.close()
file_uni_pub.close()
file_uni_priv.close()

# 암호화 (유니티의 공개키로 암호화)
# print 할때 repr
def PyEncryption(str):
    enc_data = uni_pub_key.encrypt(str.encode(), 32)[0]
   # print(type(enc_data))
  #  print('Encoded\n', repr(enc_data))
    return repr(enc_data)

# 복호화 (파이썬의 개인키로 복호화)
def PyDecryption(str):
    dec_data = py_priv_key.decrypt(str).decode()
    #print(type(dec_data))
   # print('Decoded\n', repr(dec_data))
    return repr(dec_data)

# 테스트 암호화 (파이썬의 공개키로 암호화)
def TestEncryption(str):
    enc_test = uni_pub_key.encrypt(str.encode(), 32)[0]
    return repr(enc_test)

# 테스트 복호화 (유니티의 개인키로 복호화)
def TestDecryption(str):  
    dec_test = uni_priv_key.decrypt(str).decode()
    return repr(dec_test)
