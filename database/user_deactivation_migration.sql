ALTER TABLE `users`
  ADD COLUMN `is_active` tinyint(1) NOT NULL DEFAULT 1 AFTER `role_id`;