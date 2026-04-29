CREATE EXTENSION IF NOT EXISTS postgis;

ALTER TABLE "tourism_spots"
ADD COLUMN "coordinates" geography(Point, 4326);

UPDATE "tourism_spots"
SET "coordinates" = ST_SetSRID(
    ST_MakePoint("longitude"::double precision, "latitude"::double precision),
    4326
  )::geography
WHERE "coordinates" IS NULL;

CREATE OR REPLACE FUNCTION sync_tourism_spot_coordinates()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  NEW."coordinates" := ST_SetSRID(
    ST_MakePoint(NEW."longitude"::double precision, NEW."latitude"::double precision),
    4326
  )::geography;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "tourism_spots_sync_coordinates" ON "tourism_spots";

CREATE TRIGGER "tourism_spots_sync_coordinates"
BEFORE INSERT OR UPDATE OF "latitude", "longitude"
ON "tourism_spots"
FOR EACH ROW
EXECUTE FUNCTION sync_tourism_spot_coordinates();

CREATE INDEX "tourism_spots_coordinates_gix"
ON "tourism_spots"
USING GIST ("coordinates");
