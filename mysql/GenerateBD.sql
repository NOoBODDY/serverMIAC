USE mydb;
DROP TABLE IF EXISTS chat;
DROP TABLE IF EXISTS params;
DROP TABLE IF EXISTS doctorpatient;
DROP TABLE IF EXISTS patient;
DROP TABLE IF EXISTS doctor;
DROP TABLE IF EXISTS shablonparams;
DROP TABLE IF EXISTS infopatient;
CREATE TABLE doctor
(
id int auto_increment primary key,
login varchar(50) UNIQUE,
pass varchar(50),
FirstName varchar(50) NOT NULL,
Surname varchar(50) NOT NULL,
LastName varchar(50),
avatar MEDIUMBLOB
);

CREATE TABLE patient
(
Id int auto_increment primary key,
FirstName varchar(50),
Surname varchar(50),
LastName varchar(50),
PhoneNum varchar(15) UNIQUE,
snils varchar(14),
CodeNum varchar(4),
token varchar(32)
);

CREATE TABLE doctorpatient
(
patientID int references pacient(id),
doctorID int references doctor(id),
primary key (patientID, doctorID)
);

CREATE TABLE params
(
id int auto_increment primary key,
patientId int  NOT NULL references patient(Id),
unixtime int unsigned  NOT NULL,
Tag varchar(50),
topPress int NOT NULL,
lowPress int NOT NULL,
pulse int NOT NULL,
Saturation int
);


CREATE TABLE chat
(
id int auto_increment primary key,
senderID varchar(50) NOT NULL,
adresatID varchar(50),
message TEXT NOT NULL,
unixtime INT UNSIGNED NOT NULL
);

CREATE TABLE infopatient
(
snils varchar(14) PRIMARY KEY,
isIrrationalEating bit(1),
age int,
fat bit(1), 
female bit(1), 
smoking bit(1), 
diabetes bit(1), 
weight int, 
research bit(1), 
leftVentricularHypertension bit(1), 
thickeningCarotidArteryWall bit(1), 
increasedStiffnessArteryWall bit(1), 
moderateIncreaseInSerumCreatinine bit(1), 
decreaseFiltrationRate bit(1)
);
