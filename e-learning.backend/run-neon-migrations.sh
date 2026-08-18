#!/bin/bash
DB_URI="postgresql://neondb_owner:npg_qoDKlbhajB86@ep-polished-mode-azncieoj.c-3.ap-southeast-1.aws.neon.tech/neondb?sslmode=require"

docker exec -i lms-postgres-local psql "$DB_URI" -c "CREATE TABLE IF NOT EXISTS schema_migrations (
    id SERIAL PRIMARY KEY,
    filename VARCHAR(255) UNIQUE NOT NULL,
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);"

for file in $(find database -maxdepth 1 -type f -name 'V*.sql' | sort -V); do
    filename=$(basename "$file")
    applied=$(docker exec -i lms-postgres-local psql "$DB_URI" -tAq -c "SELECT COUNT(1) FROM schema_migrations WHERE filename = '$filename'" </dev/null)
    if [ "$applied" = "0" ]; then
        echo "Applying $filename..."
        if docker exec -i lms-postgres-local psql -v ON_ERROR_STOP=1 "$DB_URI" < "$file"; then
            docker exec -i lms-postgres-local psql "$DB_URI" -q -c "INSERT INTO schema_migrations (filename) VALUES ('$filename')" </dev/null
            echo "Done $filename"
        else
            echo "Failed $filename"
            exit 1
        fi
    else
        echo "Skipped $filename"
    fi
done
echo "All migrations applied."
