from aiogram.fsm.state import StatesGroup, State

class TestCreate(StatesGroup):
    create=State()
    
class TestCheck(StatesGroup):
    check=State()