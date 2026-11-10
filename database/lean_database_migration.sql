-- Lean Database Migration
-- BeanTrack: Streamlining product inventory and eliminating write-only receipt tables

USE `beantrack`;

-- 1. Add quantity directly to products table if not exists
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'beantrack' 
      AND TABLE_NAME = 'products' 
      AND COLUMN_NAME = 'quantity'
);

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `products` ADD COLUMN `quantity` INT(11) NOT NULL DEFAULT 0 AFTER `unit_price`', 
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Migrate current on-hand stock from inventory table to products table
UPDATE `products` p
JOIN `inventory` i ON i.product_id = p.id
SET p.quantity = i.quantity;

-- 3. Drop redundant inventory table (quantity is now stored directly in products)
DROP TABLE IF EXISTS `inventory`;

-- 4. Drop write-only receipt tables (delivery logs are verified and tracked via audit_trail)
DROP TABLE IF EXISTS `stock_receipt_items`;
DROP TABLE IF EXISTS `stock_receipts`;
DROP TABLE IF EXISTS `ingredient_stock_receipts`;
