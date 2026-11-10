
-- 1. Branches Table
CREATE TABLE IF NOT EXISTS `branches` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `code` VARCHAR(30) NOT NULL UNIQUE,
  `name` VARCHAR(100) NOT NULL,
  `address` VARCHAR(255) NULL,
  `active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 2. Brands Table
CREATE TABLE IF NOT EXISTS `brands` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(80) NOT NULL UNIQUE,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 3. Categories / Types Table
CREATE TABLE IF NOT EXISTS `categories` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(80) NOT NULL UNIQUE,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 4. Customers Table
CREATE TABLE IF NOT EXISTS `customers` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(120) NOT NULL UNIQUE,
  `default_price_type` ENUM('SRP', 'WHOLESALE', 'BULK', 'DEALER', 'RESELLER') NOT NULL DEFAULT 'SRP',
  `contact_no` VARCHAR(50) NULL,
  `address` VARCHAR(255) NULL,
  `active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 5. Modify Products Table
ALTER TABLE `products`
  ADD COLUMN IF NOT EXISTS `brand_id` INT(11) NULL AFTER `id`,
  ADD COLUMN IF NOT EXISTS `brand` VARCHAR(80) NOT NULL DEFAULT '' AFTER `brand_id`,
  ADD COLUMN IF NOT EXISTS `qty_per_case` INT(11) NOT NULL DEFAULT 1 AFTER `category`,
  ADD COLUMN IF NOT EXISTS `unit` VARCHAR(30) NOT NULL DEFAULT 'pc' AFTER `qty_per_case`,
  ADD COLUMN IF NOT EXISTS `srp` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `unit`,
  ADD COLUMN IF NOT EXISTS `wholesale_price` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `srp`,
  ADD COLUMN IF NOT EXISTS `bulk_price` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `wholesale_price`,
  ADD COLUMN IF NOT EXISTS `dealer_price` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `bulk_price`,
  ADD COLUMN IF NOT EXISTS `reseller_price` DECIMAL(10,2) NOT NULL DEFAULT 0.00 AFTER `dealer_price`;


-- 6. Branch Inventory Table
CREATE TABLE IF NOT EXISTS `branch_inventory` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `branch_id` INT(11) NOT NULL,
  `product_id` INT(11) NOT NULL,
  `quantity` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_branch_product` (`branch_id`, `product_id`),
  KEY `fk_bi_branch` (`branch_id`),
  KEY `fk_bi_product` (`product_id`),
  CONSTRAINT `fk_bi_branch` FOREIGN KEY (`branch_id`) REFERENCES `branches` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_bi_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 7. Supplier Deliveries Table (Inbound Receiving / P.O.)
CREATE TABLE IF NOT EXISTS `supplier_deliveries` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `date_arrived` DATE NOT NULL,
  `supplier` VARCHAR(150) NOT NULL,
  `po_no` VARCHAR(80) NOT NULL,
  `branch_id` INT(11) NOT NULL,
  `branch_name` VARCHAR(50) NOT NULL DEFAULT 'DAVAO',
  `product_id` INT(11) NOT NULL,
  `brand_name` VARCHAR(80) NOT NULL,
  `item_name` VARCHAR(150) NOT NULL,
  `cases` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `qty` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `remarks` VARCHAR(255) NULL,
  `received_by` INT(11) NOT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_sd_date` (`date_arrived`),
  KEY `idx_sd_branch` (`branch_id`),
  KEY `idx_sd_product` (`product_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 8. Inventory Spoilage Table (Wastage / Damages / Discrepancies)
CREATE TABLE IF NOT EXISTS `inventory_spoilage` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `date` DATE NOT NULL,
  `branch_id` INT(11) NOT NULL,
  `branch_name` VARCHAR(50) NOT NULL DEFAULT 'DAVAO',
  `product_id` INT(11) NOT NULL,
  `brand_name` VARCHAR(80) NOT NULL,
  `item_name` VARCHAR(150) NOT NULL,
  `qty` DECIMAL(10,2) NOT NULL DEFAULT 1.00,
  `remarks` VARCHAR(255) NOT NULL,
  `logged_by` INT(11) NOT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_sp_date` (`date`),
  KEY `idx_sp_branch` (`branch_id`),
  KEY `idx_sp_product` (`product_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 9. Inventory Transfers Table (Inter-Branch Transfers)
CREATE TABLE IF NOT EXISTS `inventory_transfers` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `date` DATE NOT NULL,
  `from_branch_id` INT(11) NOT NULL,
  `from_branch_name` VARCHAR(50) NOT NULL,
  `to_branch_id` INT(11) NOT NULL,
  `to_branch_name` VARCHAR(50) NOT NULL,
  `product_id` INT(11) NOT NULL,
  `brand_name` VARCHAR(80) NOT NULL,
  `item_name` VARCHAR(150) NOT NULL,
  `qty` DECIMAL(10,2) NOT NULL DEFAULT 1.00,
  `remarks` VARCHAR(255) NULL,
  `transferred_by` INT(11) NOT NULL,
  `created_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_tr_date` (`date`),
  KEY `idx_tr_from` (`from_branch_id`),
  KEY `idx_tr_to` (`to_branch_id`),
  KEY `idx_tr_product` (`product_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 10. Modify Transactions Table
ALTER TABLE `transactions`
  ADD COLUMN IF NOT EXISTS `customer_id` INT(11) NULL AFTER `cashier_id`,
  ADD COLUMN IF NOT EXISTS `customer_name` VARCHAR(120) NOT NULL DEFAULT 'WALK-IN' AFTER `customer_id`,
  ADD COLUMN IF NOT EXISTS `branch_id` INT(11) NOT NULL DEFAULT 1 AFTER `customer_name`,
  ADD COLUMN IF NOT EXISTS `branch_name` VARCHAR(50) NOT NULL DEFAULT 'DAVAO' AFTER `branch_id`,
  ADD COLUMN IF NOT EXISTS `transaction_date` DATE NULL AFTER `branch_name`,
  ADD COLUMN IF NOT EXISTS `bundle` VARCHAR(100) NULL AFTER `payment_status`,
  ADD COLUMN IF NOT EXISTS `remarks` VARCHAR(255) NULL AFTER `bundle`;


-- 11. Modify Transaction Items Table
ALTER TABLE `transaction_items`
  ADD COLUMN IF NOT EXISTS `branch_name` VARCHAR(50) NOT NULL DEFAULT 'DAVAO' AFTER `product_id`,
  ADD COLUMN IF NOT EXISTS `brand_name` VARCHAR(80) NOT NULL DEFAULT '' AFTER `branch_name`,
  ADD COLUMN IF NOT EXISTS `product_name` VARCHAR(150) NOT NULL DEFAULT '' AFTER `brand_name`,
  ADD COLUMN IF NOT EXISTS `price_type` ENUM('SRP', 'WHOLESALE', 'BULK', 'DEALER', 'RESELLER') NOT NULL DEFAULT 'SRP' AFTER `product_name`,
  ADD COLUMN IF NOT EXISTS `ordered_qty` DECIMAL(10,2) NOT NULL DEFAULT 1.00 AFTER `price_type`,
  ADD COLUMN IF NOT EXISTS `unit_qty` DECIMAL(10,2) NOT NULL DEFAULT 1.00 AFTER `ordered_qty`,
  ADD COLUMN IF NOT EXISTS `price_seq` DATE NULL AFTER `subtotal`,
  ADD COLUMN IF NOT EXISTS `bundle_seq` VARCHAR(50) NOT NULL DEFAULT 'NOT A BUNDLE' AFTER `price_seq`,
  ADD COLUMN IF NOT EXISTS `remarks` VARCHAR(255) NULL AFTER `bundle_seq`;


-- 12. Modify Users Table
ALTER TABLE `users`
  ADD COLUMN IF NOT EXISTS `branch_id` INT(11) NULL AFTER `role_id`;
