# -*- coding: utf-8 -*-
"""
PgSQL general
"""
import datetime
import psycopg2

class PgSQL_OP(object):
    def __init__(self, db, plan_code,model_instance):
        self.host = db[0]
        self.port = int(db[1])
        self.user = db[2]
        self.passwd = db[3]
        self.db = db[4]
        self.tablename = db[5]
        self.plan_code = plan_code
        self.model_instance = model_instance

    def connect(self):
        # connect PgSQL
        my_connect = psycopg2.connect(host = self.host, port = self.port, user = self.user, password = self.passwd, dbname = self.db)
        self.connect = my_connect
        self.cursor = my_connect.cursor()

    def close(self):
        if self.cursor:
            self.cursor.close()
        if self.connect:
            self.connect.close()

    def del_old(self):
        sql = "delete from " + self.tablename + " where plan_code = '" + self.plan_code + "' and model_instance = '" + self.model_instance + "' and time = 'all_time' "
        self.cursor.execute(sql)
        self.connect.commit()

    def insert(self, result_json):
        sql = "insert into " + self.tablename + "(model_instance,plan_code,time,reach_gispolygon) values ('" + self.model_instance + "','" + self.plan_code + "','all_time', '" + result_json + "')"
        self.cursor.execute(sql)
        self.connect.commit()

    def update(self, result_json):
        self.cursor.execute("update " + self.tablename + " set result_json = '" + result_json + "' where code = '" + self.plan_code + "'")
        self.connect.commit()

    def succeed(self):
        self.cursor.execute("update model_exe set state = '1', ed_time = '" + datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S") + "' where exe_sign = 'py3_inundate' and state = '0' and rela = '" + self.plan_code + "'")
        self.cursor.execute("update " + self.tablename + " set status = 'success'" + " where code = '" + self.plan_code + "'")
        self.connect.commit()

    def fail(self):
        self.cursor.execute("update model_exe set state = '2', ed_time = '" + datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S") + "' where exe_sign = 'py3_inundate' and state = '0' and rela = '" + self.plan_code + "'")
        self.cursor.execute("update " + self.tablename + " set status = 'fail'" + " where code = '" + self.plan_code + "'")
        self.connect.commit()
