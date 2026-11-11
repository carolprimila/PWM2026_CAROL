-- Executado apenas na primeira criação do volume do PostgreSQL (docker-entrypoint-initdb.d).
-- btree_gist: necessária para as restrições de exclusão que impedem reservas sobrepostas (Etapa 4).
CREATE EXTENSION IF NOT EXISTS btree_gist;
