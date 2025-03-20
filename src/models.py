class User:
    def __init__(self, id, full_name, username):
        self.id = id
        self.full_name = full_name
        self.username = username

class Test:
    def __init__(self, id, name, test_amount, answers_json, creator_user_id, creator_user_full_name, created_at):
        self.id = id
        self.name = name
        self.test_amount = test_amount
        self.answers_json = answers_json
        self.creator_user_id = creator_user_id
        self.creator_user_full_name = creator_user_full_name
        self.created_at = created_at
        
class Answer:
    def __init__(self, id, test_id, user_id, full_name, answers_json, correct_count, incorrect_count, percentage, created_at):
        self.id = id
        self.test_id = test_id
        self.user_id = user_id
        self.full_name = full_name
        self.answers_json = answers_json
        self.correct_count = correct_count
        self.incorrect_count = incorrect_count
        self.percentage = percentage
        self.created_at = created_at
