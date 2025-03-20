import asyncpg
from config import DB_CONFIG

async def get_db_connection():
    return await asyncpg.connect(**DB_CONFIG)