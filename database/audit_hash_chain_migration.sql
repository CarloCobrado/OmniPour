ALTER TABLE `audit_trail`
  ADD COLUMN `previous_hash` char(64) DEFAULT NULL AFTER `created_at`,
  ADD COLUMN `entry_hash` char(64) DEFAULT NULL AFTER `previous_hash`;

CREATE TABLE `audit_chain_state` (
  `id` tinyint(3) unsigned NOT NULL,
  `last_id` int(11) NOT NULL,
  `chain_start_id` int(11) NOT NULL,
  `last_hash` char(64) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `audit_chain_state` (`id`, `last_id`, `chain_start_id`, `last_hash`)
SELECT 1, COALESCE(MAX(`id`), 0), COALESCE(MAX(`id`), 0) + 1, REPEAT('0', 64)
FROM `audit_trail`;