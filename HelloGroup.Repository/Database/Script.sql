CREATE DATABASE HelloGroup;
USE HelloGroup;
CREATE TABLE [Transactions] (
    TransactionId BIGINT,
    LineNumber BIGINT NULL,
    FCDebit FLOAT NULL,
	FCCredit FLOAT NULL,
	Debit FLOAT NULL,
	Credit FLOAT NULL,
	PostDate DATE NULL,
	Currency VARCHAR(50) NULL
);