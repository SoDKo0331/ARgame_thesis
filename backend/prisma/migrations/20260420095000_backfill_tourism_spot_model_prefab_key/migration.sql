UPDATE "tourism_spots"
SET "model_prefab_key" = regexp_replace("name", '[^A-Za-z0-9]+', '', 'g')
WHERE "model_prefab_key" IS NULL OR "model_prefab_key" = '';
