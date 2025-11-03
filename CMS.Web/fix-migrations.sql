-- Fix migration history by inserting missing migration records
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES 
('20250701152720_AddUserProfileDetails', '9.0.0'),
('20250701191902_AddUserBlockingSystem', '9.0.0'),
('20250701200559_AddUserBlockingFields', '9.0.0'),
('20251101212100_AddProductsTable', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;