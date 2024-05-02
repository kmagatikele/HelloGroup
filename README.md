### Instructions

#### Run on Visual Studio
1. Clone the code git clone https://github.com/kmagatikele/HelloGroup.git

2. Create database from this project HelloGroup.Repository/Database/Script.sql

3. Run the program from visual Studio 

#### Run using docker
1. Open cmd 

2. cd to Project 

3. Run this command to create image  
docker build -t hellogroup .

4. Run container 
docker run -d  --name hellogroup hellogroup:lastet
