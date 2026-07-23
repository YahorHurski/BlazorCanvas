#!/usr/bin/env bash
set -euo pipefail

DB_NAME="${CANVAS_DB:-canvas}"
DB_HOST="${CANVAS_DB_HOST:-localhost}"
DB_PORT="${CANVAS_DB_PORT:-5433}"
DB_USER="${CANVAS_DB_USER:-postgres}"
DB_PASSWORD="${CANVAS_DB_PASSWORD:-postgres}"

psql_query() {
  if command -v psql >/dev/null 2>&1; then
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -Atqc "$1"
  else
    docker exec canvas-postgres psql -U "$DB_USER" -d "$DB_NAME" -Atqc "$1"
  fi
}

assert_eq() {
  local actual="$1"
  local expected="$2"
  local label="$3"
  if [[ "$actual" != "$expected" ]]; then
    printf 'FAIL %s\nexpected: %s\nactual:   %s\n' "$label" "$expected" "$actual" >&2
    exit 1
  fi
}

columns="$(psql_query "select string_agg(column_name, ',' order by array_position(array['id','user_id','type','x','y','geometry','z'], column_name)) from information_schema.columns where table_schema='public' and table_name='figures' and column_name = any(array['id','user_id','type','x','y','geometry','z']) having count(*) = 7 and count(*) = (select count(*) from information_schema.columns where table_schema='public' and table_name='figures');")"
assert_eq "$columns" "id,user_id,type,x,y,geometry,z" "figures columns"

id_type="$(psql_query "select data_type from information_schema.columns where table_schema='public' and table_name='figures' and column_name='id';")"
assert_eq "$id_type" "uuid" "id data type"

id_default="$(psql_query "select column_default from information_schema.columns where table_schema='public' and table_name='figures' and column_name='id';")"
if [[ "$id_default" != *"gen_random_uuid()"* ]]; then
  printf 'FAIL id default\nactual: %s\n' "$id_default" >&2
  exit 1
fi

x_type="$(psql_query "select data_type from information_schema.columns where table_schema='public' and table_name='figures' and column_name='x';")"
y_type="$(psql_query "select data_type from information_schema.columns where table_schema='public' and table_name='figures' and column_name='y';")"
geometry_type="$(psql_query "select udt_name from information_schema.columns where table_schema='public' and table_name='figures' and column_name='geometry';")"
z_type="$(psql_query "select data_type from information_schema.columns where table_schema='public' and table_name='figures' and column_name='z';")"
assert_eq "$x_type" "integer" "x data type"
assert_eq "$y_type" "integer" "y data type"
assert_eq "$geometry_type" "jsonb" "geometry data type"
assert_eq "$z_type" "numeric" "z data type"

checks="$(psql_query "select string_agg(conname, ',' order by conname) from pg_constraint where conrelid='public.figures'::regclass and contype='c';")"
assert_eq "$checks" "figures_type_is_known" "figures check constraints"

geometry_checks="$(psql_query "select count(*) from pg_constraint where conrelid='public.figures'::regclass and contype='c' and pg_get_constraintdef(oid) ilike '%geometry%';")"
assert_eq "$geometry_checks" "0" "no geometry CHECK"

created_at_count="$(psql_query "select count(*) from information_schema.columns where table_schema='public' and table_name='figures' and column_name='created_at';")"
assert_eq "$created_at_count" "0" "no created_at column"

index_columns="$(psql_query "select string_agg(a.attname, ',' order by k.ord) from pg_class i join pg_index ix on ix.indexrelid=i.oid join lateral unnest(ix.indkey) with ordinality as k(attnum, ord) on true join pg_attribute a on a.attrelid=ix.indrelid and a.attnum=k.attnum where i.relname='ix_figures_user_id_z';")"
assert_eq "$index_columns" "user_id,z" "ix_figures_user_id_z columns"

migration="$(psql_query "select count(*) from \"__EFMigrationsHistory\" where \"MigrationId\" like '%AnchorGeometryRewrite';")"
assert_eq "$migration" "1" "AnchorGeometryRewrite migration history"

printf 'D-59 schema verified on %s:%s/%s: uuid id default gen_random_uuid(), integer x/y, jsonb geometry, numeric z, figures_type_is_known only, ix_figures_user_id_z, no created_at, no geometry CHECK.\n' "$DB_HOST" "$DB_PORT" "$DB_NAME"
