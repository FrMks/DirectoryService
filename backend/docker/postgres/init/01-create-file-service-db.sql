SELECT 'CREATE DATABASE file_service_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'file_service_db')\gexec
