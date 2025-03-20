import asyncio
from bot_instance import bot, dp
from handlers import register_handlers  

async def main():
    print("Starting...")
    
    # Register handlers before polling
    register_handlers(dp)
    
    await dp.start_polling(bot)

if __name__ == "__main__":
    asyncio.run(main())
