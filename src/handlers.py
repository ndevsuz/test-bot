from models import User as BotUSer
from functions import load_messages, check, check_member, extract_answers, calculate_correct_answer_percentage
from aiogram import Router, F
from aiogram.filters import Command
from aiogram.fsm.context import FSMContext
from aiogram.types import *
import services.test_service as tests
import services.answer_service as answers_service
import keyboards
from states import TestCreate, TestCheck
from config import BOT_USERNAME
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

router = Router()

admins = [1141084852, 6177562485]
MAX_MSG_LENGTH = 4000

@router.message(Command("start"))
async def welcome(message: Message):
    logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
    try:
        messages = load_messages()
        
        if not await check(message):
            return
                
        await message.answer(messages["main_menu"], reply_markup=keyboards.main_menu)
    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        logger.error(f"Error in start handler: {e}")

@router.message(F.text=="nigga")
async def say_nigga(message: Message):
    logger.info(F"[OOOOOOOOOOO NIGGAAAAAAAAAAAAA] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
    await message.answer("nigga")
    

@router.callback_query(F.data=="check_subscription")
async def check_subscription(callback: CallbackQuery):
    try:
        if not await check_member(callback.from_user.id):
            await callback.answer("❌")
            return
        await callback.answer("Muvaffaqiyatli✅")
        await callback.message.delete()
        messages = load_messages()
        await callback.message.answer(messages["welcome"])
        await callback.message.answer(messages["main_menu"], reply_markup=keyboards.main_menu)
    except Exception as e:
        logger.error(f"Error in check_subscription: {e}")
    
@router.message(F.text=="➕Test yaratish")
async def try_create_test(message: Message, state: FSMContext):
    try: 
        logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
        if not await check(message):
            return
        message_text = (
            "<b>❗️Yangi test yaratish!</b>\n\n"
            "<b>✅Test nomini kiritib * (yulduzcha) belgisini qo'yasiz va barcha kalitni kiritasiz.</b>\n\n"
            "<b>✍️Misol uchun:</b>\n"
            "<blockquote>Matematika*abcdabcdabcd...  yoki\nMatematika*1a2b3c4d5a6b7c...</blockquote>"
        )

        await message.reply(text=message_text,parse_mode="HTML", reply_markup=keyboards.cancel_keyboard)
        await state.set_state(TestCreate.create)
    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        logger.error(f"Error in try_create_test: {e}")
    
@router.message(TestCreate.create)
async def create_test(message: Message, state: FSMContext):
    logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
    try:
        if not await check(message):
            return
        if message.text == "Bekor qilish❌":
            await message.answer("❌ Bekor qilindi.", reply_markup=keyboards.main_menu)
            await state.clear()
            return
        messages = load_messages
        test_parts = message.text.split("*")
        if len(test_parts) != 2 or not test_parts[0].strip() or not test_parts[1].strip():
            await message.answer(
                "⚠️ Iltimos, to'g'ri formatda kiriting. Misol:\n\n"
                "<code>Matematika*1a2b3c</code>\n\n"
                "Test nomi va javoblar * bilan ajratilishi shart.",
                parse_mode="HTML", reply_markup=keyboards.cancel_keyboard
            )
            return  # Wait for the user to retry
        test_name, answers_text = test_parts[0].strip(), test_parts[1].strip()

        answers_dict = extract_answers(answers_text)  # This converts "1a2b3c" into {"1": "a", "2": "b", ...}

        test_id = await tests.create(
            name=test_name,
            test_amount=len(answers_dict),  # The number of answers in the test
            answers=answers_dict,  # Store answers as JSON
            creator_user_id=message.from_user.id,
            creator_user_full_name=message.from_user.full_name
        )

        if not test_id:
            await message.answer(messages["error"])  # Handle DB failure case
            print('test id is null')
            return
        
        test = await tests.get_by_id(test_id)
        
        success_message = f"✅Test bazaga qo`shildi. \nTest kodi: {test.id} \n Savollar soni: {test.test_amount} ta.\nQuyidagi tayyor izohni o'quvchilaringizga yuborishingiz mumkin\n👇👇👇"
        await message.reply(success_message)
        
        message_text = (
        "<b>📝📝Test boshlandi!</b>\n\n"
        "<b>Test muallifi:\n</b>\n"
        f'<a href="tg://user?id={test.creator_user_id}">{message.from_user.full_name}</a>\n\n'
        f"Fan: {test.name}\n"
        f"Savollar soni: {test.test_amount}\n"
        f"Test kodi: {test.id}\n\n\n"
        f"Javoblaringizni {BOT_USERNAME} ga quyidagi ko'rinishlarda yuborishingiz mumkin:\n\n"
        f"<blockquote>{test.id}*abcdabcd...</blockquote>\n"
        "yoki\n"
        f"<blockquote>{test.id}*1a2b3c4d5a...</blockquote>\n"
        )
        await message.answer(
            text=message_text,
            parse_mode="HTML",
            reply_markup=keyboards.main_menu
        )

        await state.clear()
    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        print(f"Error in create_test: {e}")



@router.message(F.text=="✅Javobni tekshirish")
async def check_test_button_pressed(message: Message, state: FSMContext):
    try:
        logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
        if not await check(message):
            return

        message_text = (
            "<b>✅Test kodini kiritib * (yulduzcha) belgisini qo'yasiz va barcha kalitni kiritasiz.</b>\n\n"
            "<b>✍️Misol uchun: </b>"
            "<blockquote>123*abcdabcd...</blockquote>\n"
            "yoki\n"
            "<blockquote>123*1a2b3c4d5a...</blockquote>\n"
        )
        await message.reply(text=message_text, reply_markup=keyboards.cancel_keyboard, parse_mode="HTML")
        await state.set_state(TestCheck.check)
    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        logger.error(f"Error in check_test_button_pressed: {e}")
    

@router.message(TestCheck.check)
async def process_test_answers(message: Message, state: FSMContext):
    logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
    
    try:
        if not await check(message):
            return
        if message.text == "Bekor qilish❌":
            await message.answer("❌ Bekor qilindi.", reply_markup=keyboards.main_menu)
            await state.clear()
            return

        # 🔥 Validate input format
        if "*" not in message.text:
            await message.answer("⚠️ Iltimos, test kodini va javoblarni * bilan ajratib kiriting.", reply_markup=keyboards.cancel_keyboard)
            return

        parts = message.text.split("*")
        if len(parts) != 2 or not parts[0].isdigit():
            await message.answer("⚠️ Xato format. Misol: <code>123*1a2b3c</code>", parse_mode="HTML")
            return

        test_id, user_answers_text = int(parts[0]), parts[1]

        # 🔥 Fetch test from DB
        test = await tests.get_by_id(test_id)
        if not test:
            await message.answer("❌ Bunday test topilmadi.")
            return

        # 🔥 Convert user answers to dictionary format
        user_answers_dict = extract_answers(user_answers_text)

        if not test.test_amount == len(user_answers_dict):
            await message.answer(f"{test.id} kodli testda savollar soni {test.test_amount} ta.\n❌Siz esa {len(user_answers_dict)} ta javob yozdingiz!")
            return

        percentage, correct_count, incorrect_count, incorrect_answers = calculate_correct_answer_percentage(
            user_answers_dict, test.answers_json
        )

        answer_id = await answers_service.create(
            test_id=test.id,
            user_id=message.from_user.id,
            full_name=message.from_user.full_name,
            answers=user_answers_dict,
            correct_count=correct_count,
            incorrect_count=incorrect_count,
            percentage=percentage
        )

        result_message = (
            f"👤 <b>Foydalanuvchi:\n</b> <a href='tg://user?id={message.from_user.id}'>{message.from_user.full_name}</a>\n\n\n"
            f"<b>Test nomi:</b> {test.name}\n"
            f"<b>Test kodi: {test.id}</b>\n"
            f"<b>Jami savollar soni:</b> {test.test_amount}\n"
            f"<b>To'g'ri javoblar soni:</b> {correct_count}\n"
            f"<b>Foiz:</b> {int(percentage)}%\n\n\n"
            "☝️ Noto`g`ri javoblaringiz test yakunlangandan so'ng yuboriladi.\n\n"
            "-------------------\n"
            f"🕐 Sana, vaqt: {DateTime.now()}"   
        )

        await message.answer(result_message, parse_mode="HTML", reply_markup=keyboards.main_menu)

        await notify_creator(test, message, correct_count, percentage, incorrect_answers )

        await state.clear()
    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        logger.error(f"Error in process_test_answers: {e}")

def register_handlers(dp):
    dp.include_router(router) 


async def notify_creator(test, message: Message, correct_count: int, percentage: float, incorrect_answers: list):
    """Send test results to the creator of the test."""
    creator_id = test.creator_user_id

    incorrect_text = ", ".join(map(str, incorrect_answers)) if incorrect_answers else "✅ Barcha javoblar to'g'ri!"

    result_message = (
        f"<a href='tg://user?id={message.from_user.id}'>{message.from_user.full_name}</a> "
        f"<b>{test.id}</b> kodli testning javoblarini yubordi.\n\n\n"
        f"<b>Natijasi:</b> {correct_count} ta / {test.test_amount} tadan\n"
        f"<b>Xatolari:</b> {incorrect_text}\n"
        f"<b>Foiz:</b> {int(percentage)}%\n\n"
        "-------------------\n"
        f"/joriyholat_{test.id}\n\n"
        f"/yakunlash_{test.id}"
    )

    try:
        await message.bot.send_message(creator_id, result_message, parse_mode="HTML")
    except Exception as e:
        logger.error(f"[!] Failed to notify creator: {e}")


@router.message(F.text.startswith("/joriyholat_"))
async def get_test_results(message: Message):
    try:
        logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
        if not await check(message):
            return
        # 🔥 Extract test ID from the command
        test_id = message.text.replace("/joriyholat_", "").strip()
        if not test_id.isdigit():
            await message.answer("⚠️ Test ID noto‘g‘ri kiritildi.", reply_markup=keyboards.main_menu)
            return
        
        test_id = int(test_id)

        # 🔥 Fetch the test from DB
        test = await tests.get_by_id(test_id)
        if not test:
            await message.answer("❌ Bunday test topilmadi.", reply_markup=keyboards.main_menu)
            return
        
        print("creator_user_id:", test.creator_user_id, type(test.creator_user_id))
        print("from_user.id:", message.from_user.id, type(message.from_user.id))
        print("is admin:", message.from_user.id in admins)

        if test.creator_user_id != message.from_user.id and message.from_user.id not in admins:
            await message.answer("⛔️ Siz faqat o‘zingiz yaratgan testni ko'rishingiz mumkin!", reply_markup=keyboards.main_menu)
            return

        # 🔥 Fetch all answers for this test
        answers = await answers_service.get_all_by_test(test_id)
        if not answers:
            await message.answer("🚫 Hali hech kim bu testda qatnashmadi.")
            return

        # 🔥 Sort users by correct answers (descending)
        sorted_answers = sorted(answers, key=lambda x: x.correct_count, reverse=True)

        # 🔥 Prepare the results message

        await send_test_results_with_chunking(message, test, sorted_answers)

    except Exception as e:
        logger.error(f"[!] Error in get_test_results: {e}")
        await message.answer("❌ Xatolik yuz berdi. Keyinroq qayta urinib ko‘ring.", reply_markup=keyboards.main_menu)

@router.message(F.text.startswith("/yakunlash_"))
async def finalize_test(message: Message):
    try:
        logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
        if not await check(message):
            return
        # 🔥 Extract test ID from the command
        test_id = message.text.replace("/yakunlash_", "").strip()
        if not test_id.isdigit():
            await message.answer("⚠️ Test ID noto‘g‘ri kiritildi.", reply_markup=keyboards.main_menu)
            return

        test_id = int(test_id)

        # 🔥 Fetch the test from DB
        test = await tests.get_by_id(test_id)
        if not test:
            await message.answer("❌ Bunday test topilmadi.", reply_markup=keyboards.main_menu)
            return

        # 🔥 Ensure only the test creator can delete it
        if test.creator_user_id != message.from_user.id and message.from_user.id not in admins:
            await message.answer("⛔️ Siz faqat o‘zingiz yaratgan testni yakunlashingiz mumkin!", reply_markup=keyboards.main_menu)
            return

        # 🔥 Fetch all answers for this test
        answers = await answers_service.get_all_by_test(test_id)

        # 🔥 If no one answered, delete it silently
        if not answers:
            await answers_service.delete_by_test(test_id)
            await tests.delete(test_id)
            await message.answer("📌 Hech kim testda qatnashmadi. Test o‘chirildi.", reply_markup=keyboards.main_menu)
            return

        # 🔥 Sort users by correct answers (descending)
        sorted_answers = sorted(answers, key=lambda x: x.correct_count, reverse=True)

        # 🔥 Format correct answers in "1.a  2.b  ..." style
        correct_answers_text = "   ".join([f"{k}.{v}" for k, v in test.answers_json.items()])
        result_text += f"\n<b>To‘g‘ri javoblar:</b>\n{correct_answers_text}"

        await send_test_results_with_chunking(message, test, sorted_answers, is_final=True)

        # 🔥 Notify each participant with their incorrect answers
        for answer in sorted_answers:
            # 🔥 Calculate incorrect answers dynamically
            incorrect_answers = [q for q, user_ans in answer.answers_json.items() if test.answers_json.get(q) != user_ans]
            incorrect_text = ", ".join(map(str, incorrect_answers)) if incorrect_answers else "✅ Barcha javoblar to'g'ri!"
            
            user_message = (
                "⛔️ <b>Test yakunlandi.</b>\n"
                f"<b>Test nomi:</b> {test.name}\n\n\n"
                f"<b>Xato javoblaringiz:</b> <blockquote>{incorrect_text}</blockquote>\n"
            )
            try:
                await message.bot.send_message(answer.user_id, user_message, parse_mode="HTML")
            except Exception as e:
                print(f"[!] Failed to send result to {answer.user_id}: {e}")

        # 🔥 Delete the test and all answers
        await answers_service.delete_by_test(test_id)
        await tests.delete(test_id)

    except Exception as e:
        logger.error(f"[!] Error in finalize_test: {e}")
        await message.answer("❌ Xatolik yuz berdi. Keyinroq qayta urinib ko‘ring.")
        
@router.message(F.text == "📝Testlarni ko'rish")
async def get_tests(message: Message, state: FSMContext):
    try:
        if not await check(message):
            return
        
        user_tests = await tests.get_by_user_id(message.from_user.id)

        if not user_tests:
            await message.answer("📭 Siz hali hech qanday test yaratmagansiz.")
            return

        # 🔥 Format test list
        response = "🧾 <b>Siz yaratgan testlar ro'yxati:</b>\n\n\nTest nomi || Test kodi:\n<blockquote expandable>"
        for test in user_tests:
            response += f"{test.name} --> {test.id}\n"
        response +="</blockquote>"

        await message.answer(response, parse_mode="HTML")

    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        logger.error(f"Error in get_tests: {e}")


@router.message()
async def default_handler(message: Message, state: FSMContext):
    try:
        logger.info(F"[LOG] user_id: {message.from_user.id}, user_name: {message.from_user.username} :  {message.text}")
        if not await check(message):
            return
        """Handles all unrecognized messages and sends the main menu."""
        await message.answer("Asosiy menu.", reply_markup=keyboards.main_menu)
        await state.clear()
    except Exception as e:
        messages = load_messages()
        await message.answer(messages["error"])
        logger.error(f"Error in default handler: {e}")
        

async def send_test_results_with_chunking(message, test, sorted_answers, is_final=False):
    result_text = (
        f"{'⛔️ <b>Test yakunlandi.</b>' if is_final else '📊 <b>Natijalarning joriy holati.</b>'}\n\n\n"
        f"<b>Test muallifi:</b>\n<a href='tg://user?id={test.creator_user_id}'>{test.creator_user_full_name}</a>\n\n"
        f"<b>Test kodi:</b> {test.id}\n"
        f"<b>Savollar soni:</b> {test.test_amount} ta\n\n"
        "✅ <b>Natijalar:</b>\n\n"
    )

    for index, answer in enumerate(sorted_answers, start=1):
        medal = " 🥇" if index == 1 else ""
        line = f"{index}. <a href='tg://user?id={answer.user_id}'>{answer.full_name}</a> - {answer.correct_count} ta{medal}\n"

        # 🔪 Split before it gets too long
        if len(result_text) + len(line) > MAX_MSG_LENGTH:
            await message.answer(result_text, parse_mode="HTML")
            result_text = ""  # Start a new chunk

        result_text += line

    # Final flush
    if is_final:
        correct_answers_text = "   ".join([f"{k}.{v}" for k, v in test.answers_json.items()])
        if len(result_text) + len(correct_answers_text) + 50 > MAX_MSG_LENGTH:
            await message.answer(result_text, parse_mode="HTML")
            result_text = ""
        result_text += f"\n<b>To‘g‘ri javoblar:</b>\n{correct_answers_text}"

    await message.answer(result_text, parse_mode="HTML")
