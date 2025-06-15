-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Jun 15, 2025 at 10:33 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `kwiatluxedb`
--

-- --------------------------------------------------------

--
-- Table structure for table `cartproducts`
--

CREATE TABLE `cartproducts` (
  `CartId` int(11) NOT NULL,
  `ProductId` int(11) NOT NULL,
  `Quantity` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `cartproducts`
--

INSERT INTO `cartproducts` (`CartId`, `ProductId`, `Quantity`) VALUES
(14, 5, 2);

-- --------------------------------------------------------

--
-- Table structure for table `carts`
--

CREATE TABLE `carts` (
  `Id` int(11) NOT NULL,
  `UserId` int(11) NOT NULL,
  `TotalAmount` decimal(18,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `carts`
--

INSERT INTO `carts` (`Id`, `UserId`, `TotalAmount`) VALUES
(14, 4, 88.80);

-- --------------------------------------------------------

--
-- Table structure for table `orderproducts`
--

CREATE TABLE `orderproducts` (
  `OrderId` int(11) NOT NULL,
  `ProductId` int(11) NOT NULL,
  `Quantity` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `orderproducts`
--

INSERT INTO `orderproducts` (`OrderId`, `ProductId`, `Quantity`) VALUES
(10, 5, 2);

-- --------------------------------------------------------

--
-- Table structure for table `orders`
--

CREATE TABLE `orders` (
  `Id` int(11) NOT NULL,
  `UserId` int(11) NOT NULL,
  `OrderDate` datetime NOT NULL,
  `TotalAmount` decimal(18,2) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `orders`
--

INSERT INTO `orders` (`Id`, `UserId`, `OrderDate`, `TotalAmount`) VALUES
(10, 4, '2025-06-15 15:27:03', 88.80);

-- --------------------------------------------------------

--
-- Table structure for table `products`
--

CREATE TABLE `products` (
  `Id` int(11) NOT NULL,
  `ProductName` varchar(100) NOT NULL,
  `ProductDescription` varchar(500) DEFAULT NULL,
  `ProductPrice` decimal(18,2) NOT NULL,
  `FileImageUrl` varchar(200) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `products`
--

INSERT INTO `products` (`Id`, `ProductName`, `ProductDescription`, `ProductPrice`, `FileImageUrl`) VALUES
(2, 'string', 'string', 18.30, 'string'),
(3, 'string2', 'string', 0.00, 'string'),
(4, 'string', 'string', 0.00, 'string'),
(5, 'test', 'test2', 44.40, NULL);

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `Id` int(11) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `Password` varchar(64) NOT NULL,
  `Salt` varchar(44) NOT NULL,
  `Email` varchar(100) NOT NULL,
  `Role` varchar(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`Id`, `Username`, `Password`, `Salt`, `Email`, `Role`) VALUES
(1, 'test', 'tZ9QGrKUJPZocZU/3ZxNoiwzW66e23s6U4CC3GUSyYY=', 'cBUgCMYeJjvG/gRSI9MXAqkVkMRRybVsa0BtoF4pQdw=', 'test@gmail.com', 'User'),
(4, 'string', 'doilx/zmtOQEiuVkglbcOzgWQgwVtdHgwfF8zCS9GBA=', 'Qm7fzCCR+7i2WgOE334vP7iSwd7YmCL6qMMdRcJg8E4=', 'string', 'string'),
(5, 'str2ing', 'QzYfS4DmFxQLRHXJYy9TKH9RdGUcPolBEv5dCNEL1yg=', 'xv5tXKed2m6cAjZhG556PvlgFLrk7EP157VAmQecqV0=', 'string', 'string'),
(6, 'stri5532ng', 'Td3kSDBZOqVrGZlDV4zIx0ap6Fgi+oeEw5ogUOxQUj0=', 'whTudQQqgxsfviRWvWD8p0CyUdbbx0hkwyYvFlipe0w=', 'string', 'string'),
(7, 'st44ring', 'ydOtZf+XfaFdoqk3Ch/2HXMtFtO/LPThyWmEjNmv8mI=', 'O+flXxuIQJbh0CWIq3PU+3tarj3zcVWq08JfpRGDqPg=', 'string', 'string'),
(8, 'real', '64qQgf6ZtuDQkrAbPHTvzaGDAlk5uVryd4CA5kmkAcE=', 'jz0np9Ucuh3SKInprxnjpm3bmxBbKkzycaTZrSVW+uA=', 'real@real.com', 'Customer'),
(9, 'real', 'pjCikvXjHJtWjEWMVwId/x9E4NY6T6bHUqObwEIzq50=', 'VAFFtNhpqXT1GKBmOUCVBaTM10QVNWdVDKboXH+wWoc=', 'real@real.com', 'Customer'),
(10, 'real2', 'Dxo7fWKcewmECAwOoLR0K5mr1XADDcrL4Wurpa+Hzto=', 'cpqUGOam6hnGNZR8LY9Loo063MB/Vtqr/5yq3cl6cYo=', 'real@real.com', 'Customer'),
(11, 'test', 'vs4v7N3gz/NOyFVFnH3yiuQ2SarBuNs5eJ82CZy6XO0=', 'GuZlw+d/ZtYqZhgkBHmq9Ul893zsLrILrFylTK9ewew=', 'test@test.com', 'Customer'),
(12, 'test4', 'HMvTiz3rElnm2HR94/SoVMnzK0zWNxy/xUjopBqGd9g=', 'h4/GgrkSzey2alq69hoKh/sjxrquG4JvgFWEIArNCuM=', 'test4@test.com', 'Customer'),
(13, '1234', 'PEhyGfIbQV74J5xgae/NusBds+KBI9mjmlhYtNTgcpU=', '4OCVwC1eSHkIKKOJNrFHAxsO3meqI5wNeu1Fhw3KmSM=', '1234@gm.com', 'Customer');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `cartproducts`
--
ALTER TABLE `cartproducts`
  ADD PRIMARY KEY (`CartId`,`ProductId`),
  ADD KEY `FK_CartProducts_ProductId` (`ProductId`);

--
-- Indexes for table `carts`
--
ALTER TABLE `carts`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `FK_Carts_UserId` (`UserId`);

--
-- Indexes for table `orderproducts`
--
ALTER TABLE `orderproducts`
  ADD PRIMARY KEY (`OrderId`,`ProductId`),
  ADD KEY `FK_Products_ProductId` (`ProductId`);

--
-- Indexes for table `orders`
--
ALTER TABLE `orders`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `FK_Orders_Users` (`UserId`);

--
-- Indexes for table `products`
--
ALTER TABLE `products`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `idx_product_name` (`ProductName`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`Id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `carts`
--
ALTER TABLE `carts`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=15;

--
-- AUTO_INCREMENT for table `orders`
--
ALTER TABLE `orders`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `products`
--
ALTER TABLE `products`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=14;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `cartproducts`
--
ALTER TABLE `cartproducts`
  ADD CONSTRAINT `FK_CartProduct_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `products` (`Id`),
  ADD CONSTRAINT `FK_Carts_CartId` FOREIGN KEY (`CartId`) REFERENCES `carts` (`Id`);

--
-- Constraints for table `carts`
--
ALTER TABLE `carts`
  ADD CONSTRAINT `FK_Carts_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE;

--
-- Constraints for table `orderproducts`
--
ALTER TABLE `orderproducts`
  ADD CONSTRAINT `FK_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`Id`),
  ADD CONSTRAINT `FK_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `products` (`Id`);

--
-- Constraints for table `orders`
--
ALTER TABLE `orders`
  ADD CONSTRAINT `FK_Orders_Users` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
