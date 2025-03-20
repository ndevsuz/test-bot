from database import get_db_connection 
from models import Test 
import json

async def create(name: str, test_amount: int, answers: dict, creator_user_id: int, creator_user_full_name: str):
    """Create a new test in the database."""
    conn = await get_db_connection()
    query = """
    INSERT INTO sys_test (name, test_amount, answers_json, creator_user_id, creator_user_full_name, created_at)
    VALUES ($1, $2, $3, $4, $5, NOW())
    RETURNING id
    """
    try:
        # 🔥 Convert dict to a correctly formatted JSON string
        answers_json = json.dumps(answers, ensure_ascii=False)
        test_id = await conn.fetchval(query, name, test_amount, answers_json, creator_user_id, creator_user_full_name)
        return test_id
    except Exception as e:
        print(f"[!] Error creating test: {e}")
    finally:
        await conn.close()

async def get_by_id(test_id: int):
    """Retrieve a test by its ID."""
    conn = await get_db_connection()
    query = "SELECT * FROM sys_test WHERE id = $1"
    try:
        row = await conn.fetchrow(query, test_id)
        if row:
            return Test(
                id=row["id"],
                name=row.get("name"),  # Use `.get()` to prevent KeyErrors
                test_amount=row.get("test_amount"),
                answers_json=json.loads(row.get("answers_json", "{}")),  # Default to empty dict
                creator_user_id=row.get("creator_user_id"),
                creator_user_full_name=row.get("creator_user_full_name"),
                created_at=row.get("created_at")
            )
    except Exception as e:
        print(f"[!] Error retrieving test: {e}")
    finally:
        await conn.close()

async def get_all():
    """Retrieve all tests."""
    conn = await get_db_connection()
    query = "SELECT * FROM sys_test"
    try:
        rows = await conn.fetch(query)
        return [
            Test(
                id=row["id"],
                name=row["name"],
                test_amount=row["test_amount"],
                answers_json=json.loads(row["answers_json"]),  # 🔥 Convert JSON string to dict
                creator_user_id=row["creator_user_id"],
                creator_user_full_name=row["creator_user_full_name"],
                created_at=row["created_at"]
            )
            for row in rows
        ]
    except Exception as e:
        print(f"[!] Error retrieving all tests: {e}")
    finally:
        await conn.close()

async def delete(test_id: int):
    """Delete a test by its ID."""
    conn = await get_db_connection()
    query = "DELETE FROM sys_test WHERE id = $1"
    try:
        await conn.execute(query, test_id)
    except Exception as e:
        print(f"[!] Error deleting test: {e}")
    finally:
        await conn.close()
