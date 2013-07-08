/*
Bunny & Hare GunZDB V2.1
Updated: 9/9/2010 6:23:13 AM
*/

SET FOREIGN_KEY_CHECKS=0;
-- ----------------------------
-- Table structure for account
-- ----------------------------
CREATE TABLE `account` (
  `aid` int(11) NOT NULL AUTO_INCREMENT,
  `userid` varchar(25) NOT NULL,
  `password` varchar(64) NOT NULL,
  `regdate` date DEFAULT NULL,
  `name` varchar(25) NOT NULL,
  `email` varchar(128) NOT NULL,
  `access` smallint(6) DEFAULT '0',
  `premium` smallint(6) DEFAULT '0',
  `registerip` varchar(25) DEFAULT NULL,
  `lastloginip` varchar(25) DEFAULT NULL,
  `lastconndate` datetime DEFAULT NULL,
  `age` smallint(6) DEFAULT NULL,
  `sex` tinyint(4) DEFAULT NULL,
  `address` varchar(64) DEFAULT NULL,
  `zip` int(11) DEFAULT NULL,
  `online` tinyint(4) NOT NULL DEFAULT '0',
  PRIMARY KEY (`aid`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Table structure for character
-- ----------------------------
CREATE TABLE `character` (
  `cid` int(11) NOT NULL AUTO_INCREMENT,
  `aid` int(11) NOT NULL,
  `name` varchar(24) NOT NULL,
  `level` int(11) NOT NULL DEFAULT '1',
  `sex` tinyint(4) NOT NULL,
  `charnum` tinyint(4) NOT NULL DEFAULT '0',
  `hair` tinyint(4) NOT NULL,
  `face` tinyint(4) NOT NULL,
  `XP` int(11) NOT NULL DEFAULT '0',
  `BP` int(11) NOT NULL DEFAULT '0',
  `head_slot` int(11) DEFAULT NULL,
  `chest_slot` int(11) DEFAULT NULL,
  `hands_slot` int(11) DEFAULT NULL,
  `legs_slot` int(11) DEFAULT NULL,
  `feet_slot` int(11) DEFAULT NULL,
  `fingerl_slot` int(11) DEFAULT NULL,
  `fingerr_slot` int(11) DEFAULT NULL,
  `melee_slot` int(11) DEFAULT NULL,
  `primary_slot` int(11) DEFAULT NULL,
  `secondary_slot` int(11) DEFAULT NULL,
  `custom1_slot` int(11) DEFAULT NULL,
  `custom2_slot` int(11) DEFAULT NULL,
  `regdate` datetime DEFAULT NULL,
  `playtime` int(11) DEFAULT NULL,
  `killcount` int(11) DEFAULT '0',
  `deathcount` int(11) DEFAULT '0',
  `online` tinyint(4) DEFAULT NULL,
  PRIMARY KEY (`cid`),
  KEY `ChFkeyaccount` (`aid`),
  CONSTRAINT `ChFkeyaccount` FOREIGN KEY (`aid`) REFERENCES `account` (`aid`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Table structure for characteritem
-- ----------------------------
CREATE TABLE `characteritem` (
  `ciid` int(11) NOT NULL AUTO_INCREMENT,
  `cid` int(11) NOT NULL,
  `itemid` int(11) NOT NULL,
  `regdate` datetime NOT NULL,
  `rentdate` datetime DEFAULT NULL,
  `renthourperiod` int(11) DEFAULT NULL,
  `cnt` int(11) DEFAULT NULL,
  PRIMARY KEY (`ciid`),
  KEY `cid` (`cid`),
  CONSTRAINT `FKLOL` FOREIGN KEY (`cid`) REFERENCES `character` (`cid`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Table structure for clan
-- ----------------------------
CREATE TABLE `clan` (
  `clid` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(24) NOT NULL,
  `exp` int(11) DEFAULT '0',
  `level` int(11) DEFAULT '0',
  `point` int(11) DEFAULT '0',
  `mastercid` int(11) NOT NULL,
  `wins` int(11) DEFAULT '0',
  `losses` int(11) DEFAULT '0',
  `draws` int(11) DEFAULT '0',
  `totalranking` int(11) DEFAULT '0',
  `lastmonthrank` int(11) DEFAULT NULL,
  `emblemurl` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`clid`),
  KEY `CTFKey` (`mastercid`),
  CONSTRAINT `CTFKey` FOREIGN KEY (`mastercid`) REFERENCES `character` (`cid`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;

-- ----------------------------
-- Table structure for clanmember
-- ----------------------------
CREATE TABLE `clanmember` (
  `cmid` int(11) NOT NULL AUTO_INCREMENT,
  `clid` int(11) NOT NULL,
  `cid` int(11) NOT NULL,
  `grade` tinyint(4) NOT NULL DEFAULT '9',
  `regdate` datetime NOT NULL,
  `contpoint` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`cmid`),
  KEY `CFkeymember` (`cid`),
  KEY `CFkeymemberclan` (`clid`),
  CONSTRAINT `CFkeymember` FOREIGN KEY (`cid`) REFERENCES `character` (`cid`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `CFkeymemberclan` FOREIGN KEY (`clid`) REFERENCES `clan` (`clid`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1;