import database
from models import User

async def get_all():
    conn = await database.get_connection()
    query = "SELECT * from sys_user"
    try:
        rows = await conn.fetch(query)
        users = [
            User(
                id=row['id'],
                full_name=row['full_name'],
                username=row['username'],
            )
            for row in rows
        ]

        return users
    except Exception as ex:
        print("[!] Error fetching all users: {ex}")
        return []
    finally:
        await conn.close()

async def get_by_id(id: int):
    conn = await database.get_db_connection()
    query = "SELECT * FROM sys_user WHERE id = $1"

    try:
        row = await conn.fetchrow(query, id)
        if not row:
            print(f"[!] No user found with id: {id}")
            return None

        user = User(
            id=row['id'],
            full_name=row['full_name'],
            username=row['username'],
        )

        return user
    except Exception as ex:
        print(f"[!] Error fetching user by id: {ex}")
        return None
    finally:
        await conn.close()

async def create(user: User):
    conn = await database.get_db_connection()
    query = "INSERT INTO sys_user(id, full_name, username) VALUES ($1, $2, $3) ON CONFLICT (id) DO NOTHING"
    #additional validation for full_name😁
    full_name = (user.full_name[:50] + "…") if len(user.full_name) > 50 else user.full_name
    try:
        await conn.execute(
            query,
            user.id,
            full_name,
            user.username,
        )
        print('new user created')
    except Exception as ex:
        print(f"[!] Eror with creating user: {ex}")
    finally:
        await conn.close()  