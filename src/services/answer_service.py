from models import Answer
from database import get_db_connection
import json

async def create(test_id: int, user_id: int, full_name: str, answers: dict, correct_count: int, incorrect_count: int, percentage: float):
    """Store user's test answers in the database."""
    conn = await get_db_connection()
    query = """
    INSERT INTO sys_answer (test_id, user_id, full_name, answers_json, correct_count, incorrect_count, percentage, created_at)
    VALUES ($1, $2, $3, $4, $5, $6, $7, NOW())
    RETURNING id
    """
    try:
        answers_json = json.dumps(answers)
        answer_id = await conn.fetchval(query, test_id, user_id, full_name, answers_json, correct_count, incorrect_count, percentage)
        return answer_id
    except Exception as e:
        print(f"[!] Error saving user answers: {e}")
    finally:
        await conn.close()
        
async def get_by_id(answer_id: int):
    """Retrieve an answer record by its ID."""
    conn = await get_db_connection()
    query = "SELECT * FROM sys_answer WHERE id = $1"
    try:
        row = await conn.fetchrow(query, answer_id)
        if row:
            return Answer(
                id=row["id"],
                test_id=row["test_id"],
                user_id=row["user_id"],
                full_name=row["full_name"],
                answers_json=json.loads(row["answers_json"]),
                correct_count=row["correct_count"],
                incorrect_count=row["incorrect_count"],
                percentage=row["percentage"],
                created_at=row["created_at"]
            )
    except Exception as e:
        print(f"[!] Error retrieving answer: {e}")
    finally:
        await conn.close()

async def get_all_by_test(test_id: int):
    """Retrieve all answers for a specific test."""
    conn = await get_db_connection()
    query = "SELECT * FROM sys_answer WHERE test_id = $1"
    try:
        rows = await conn.fetch(query, test_id)
        return [
            Answer(
                id=row["id"],
                test_id=row["test_id"],
                user_id=row["user_id"],
                full_name=row["full_name"],
                answers_json=json.loads(row["answers_json"]),
                correct_count=row["correct_count"],
                incorrect_count=row["incorrect_count"],
                percentage=row["percentage"],
                created_at=row["created_at"]
            )
            for row in rows
        ]
    except Exception as e:
        print(f"[!] Error retrieving answers for test {test_id}: {e}")
    finally:
        await conn.close()
        
async def delete(answer_id: int):
    """Delete an answer by its ID."""
    conn = await get_db_connection()
    query = "DELETE FROM sys_answer WHERE id = $1"
    try:
        await conn.execute(query, answer_id)
    except Exception as e:
        print(f"[!] Error deleting answer: {e}")
    finally:
        await conn.close()

async def delete_by_test(test_id: int):
    """Delete all answers related to a test."""
    conn = await get_db_connection()
    query = "DELETE FROM sys_answer WHERE test_id = $1"
    try:
        await conn.execute(query, test_id)
    except Exception as e:
        print(f"[!] Error deleting answers for test {test_id}: {e}")
    finally:
        await conn.close()
        
        
