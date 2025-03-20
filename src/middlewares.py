# from aiogram.types import Message
# from aiogram.dispatcher.middlewares.base import BaseMiddleware
# import services.user_service as users
# from models import User
# from functions import load_messages


# class UserMiddleware(BaseMiddleware):
#     async def __call__(self, handler, event: Message, data: dict):
#         try:
#             user_id = event.from_user.id
#             user = await users.get_by_id(user_id)

#             if not user:
#                 user = User(
#                     id=user_id,
#                     first_name=event.from_user.first_name,
#                     last_name=event.from_user.last_name,
#                 )
#                 await users.create(user)

#             # Add user to data dictionary so handlers can access it
#             data["user"] = user
#             data["messages"] = load_messages()
            
#         except Exception as e:
#             print(f"Middleware Error: {e}")
        
#         return await handler(event, data)
