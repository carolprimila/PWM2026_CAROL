# Banco de dados

O Projeto Clubes usa **SQLite**: um único arquivo, `src/ProjetoClubes.Api/projetoclubes.db`, criado pela própria
API na primeira execução (migrations do EF Core em `src/ProjetoClubes.Api/Infrastructure/Persistence/Migrations`).
Não há servidor de banco para instalar.

- Começar do zero: pare a API e apague `projetoclubes.db` (e `-wal`/`-shm`). Na próxima execução ele é recriado com os
  dados de demonstração.
- Backup: copie o arquivo com a API parada, ou use `sqlite3 projetoclubes.db ".backup copia.db"` com ela rodando.
- Seed de demonstração: ver `seed/README.md`.
