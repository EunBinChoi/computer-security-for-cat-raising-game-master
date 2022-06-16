# Computer Security for Cat-Raising Game

## What is Cat-Raising Game?
- This is the graduation project to get a bachelor's degree.
   - source: https://github.com/EunBinChoi/Gamer-Action-Classifier-LSTM-master.git
   - video: https://youtu.be/iwCqoYJd53U

## Description
Serveral security techniques are applied to this project.

### 1) Authentication of user’s action
- It is not recognized for a certain period of time in Microsoft Kinect sensor, lock the program automatically and judge whether the current incoming action is similar to the action at initial stage necessary to measure the user’s body.

### 2) Data encryption (RSA)
- Two-way communication occurs, therefore, there are two public and private key pairs.  
    * Public keys created by client (python code which implemented the neural network to classify the user’s action) and server (cat-raising game engine) respectively are shared with each other.
    * Private keys created by client and server respectively are not shared.

### 3) White list
- Maintaining a white list in server.
    * If a IP address of client which want to connect server is in the white list, allow connection.  
    *  If not, deny connection.

### 4) Code obfuscation
- Make the code difficult to understand and read, and proving protection against reverse engineering.

### 5) File encryption (CBC)
- Structure of neural network can be understand if the learning rate, hidden layer, and the number of output layer are exposed.  
    * To prevent this, use symmetric key cryptography, CBC encryption.

### 6) IP validity
- Determining the validity of a IP address which you want to connect using ip_check of netaddr module in python.
