using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;

namespace dotnetcore_angular_exam.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClassManagerController : ControllerBase
    {

        private readonly ILogger<ClassManagerController> _logger;

        public ClassManagerController(ILogger<ClassManagerController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public GetClassesResponse GetClasses(){

            tryInitDB();

            var classList = new List<Class>();
            var studentList = new List<Student>();


            using (var connection = new SqliteConnection("Data Source=conn.db")) {
                connection.Open(); 
                {
                        
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        SELECT *
                        FROM class
                    ";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetString(0);
                            var name = reader.GetString(1);

                            classList.Add(new Class(){ id = id , name = name});

                        }
                    }
                }

                {
                        
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        SELECT *
                        FROM student
                    ";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetString(0);
                            var class_id = reader.GetString(1);
                            var name = reader.GetString(2);
                            var no = reader.GetString(3);
                            var gender = reader.GetString(4);

                            studentList.Add(new Student(){ id = id , name = name
                            , class_id = class_id
                            , no = no
                            , gender = gender});

                        }
                    }
                }


            }
            
            var studentGroups = studentList.GroupBy(s => s.class_id);
            foreach(var cls in classList){
                cls.students = studentGroups.FirstOrDefault(a => a.Key == cls.id)?.ToArray();
            }



            return new GetClassesResponse(){data = classList.ToArray()};
        }

        private void tryInitDB()
        {
            if(System.IO.File.Exists("conn.db"))
                return;

            using (var connection = new SqliteConnection("Data Source=conn.db")) {
                connection.Open(); 
                {
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        CREATE TABLE class (
                            id TEXT PRIMARY KEY,
                            name TEXT NOT NULL
                        );
                    ";
                    command.ExecuteNonQuery();
                }
                {
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        CREATE TABLE student (
                            id TEXT PRIMARY KEY,
                            class_id TEXT NOT NULL,
                            name TEXT NOT NULL,
                            no TEXT NOT NULL,
                            gender TEXT NOT NULL
                        );
                    ";
                    command.ExecuteNonQuery();
                }



            }
            

        }

        [HttpPost]
        public CreateClassResponse CreateClass(CreateClassRequest request){
            tryInitDB();

            

            var data = request.data;

            data.id = Guid.NewGuid().ToString();

            using (var connection = new SqliteConnection("Data Source=conn.db")) {
                connection.Open(); 
                using(var trans = connection.BeginTransaction())
                {
                    
                    {
                            
                        var command = connection.CreateCommand();
                        command.CommandText =
                        @"
                            INSERT INTO class (id,name)
                            VALUES ($id,$name)
                        ";
                        command.Parameters.AddWithValue("$id", data.id);
                        command.Parameters.AddWithValue("$name", data.name);

                        command.ExecuteNonQuery();
                    }

                    {
                        foreach(var student in data.students){
                            student.id = Guid.NewGuid().ToString();
                            student.class_id = data.id;

                            var command = connection.CreateCommand();
                            command.CommandText =
                            @"
                                INSERT INTO student (id,class_id,name,no,gender)
                                VALUES ($id,$class_id,$name,$no,$gender)
                            ";

                            command.Parameters.AddWithValue("$id", student.id);
                            command.Parameters.AddWithValue("$class_id", student.class_id);
                            command.Parameters.AddWithValue("$name", student.name);
                            command.Parameters.AddWithValue("$no", student.no);
                            command.Parameters.AddWithValue("$gender", student.gender);

                            command.ExecuteNonQuery();

                        }
                    }

                    trans.Commit();
                }


            }

            return new CreateClassResponse(){
                status = ResponseStatus.sussess,
                data = data
            };
        }

        [HttpPut]
        public UpdateClassResponse UpdateClass(UpdateClassRequest request){
            tryInitDB();

            

            var data = request.data;

            if(String.IsNullOrEmpty(data.id))
                throw new Exception("data.id is empty or null.");

            using (var connection = new SqliteConnection("Data Source=conn.db")) {
                connection.Open(); 
                using(var trans = connection.BeginTransaction())
                {
                    
                    {
                            
                        var command = connection.CreateCommand();
                        command.CommandText =
                        @"
                            UPDATE class
                            SET name = $name 
                            WHERE id = $id
                        ";
                        command.Parameters.AddWithValue("$id", data.id);
                        command.Parameters.AddWithValue("$name", data.name);

                        command.ExecuteNonQuery();
                    }

                    {
                        //delete
                        {
                            
                            var command = connection.CreateCommand();
                            
                            var index = 0;

                            var paraNames =  new List<String>();

                            if(data.students == null)
                                data.students = new Student[]{};

                            foreach(var student in data.students){
                                if(String.IsNullOrEmpty(student.id))
                                    continue;
                                paraNames.Add($"$id{index}");
                                command.Parameters.AddWithValue($"$id{index}", student.id);
                                index++;
                            }

                            command.CommandText =
                            $@"
                                DELETE FROM student 
                                WHERE id NOT IN ({ String.Join(",",paraNames)})
                            ";

                            command.ExecuteNonQuery();
                        }
                        
                        //update
                        foreach(var student in data.students){
                            if(String.IsNullOrEmpty(student.id))
                                continue;

                            var command = connection.CreateCommand();
                            command.CommandText =
                            @"
                                UPDATE student 
                                SET name = $name,
                                    no = $no,
                                    gender = $gender
                                WHERE id = $id
                            ";

                            command.Parameters.AddWithValue("$id", student.id);
                            command.Parameters.AddWithValue("$class_id", student.class_id);
                            command.Parameters.AddWithValue("$name", student.name);
                            command.Parameters.AddWithValue("$no", student.no);
                            command.Parameters.AddWithValue("$gender", student.gender);

                            command.ExecuteNonQuery();

                        }
                        
                        //insert
                        foreach(var student in data.students){
                            if(!String.IsNullOrEmpty(student.id))
                                continue;
                            student.id = Guid.NewGuid().ToString();
                            student.class_id = data.id;

                            var command = connection.CreateCommand();
                            command.CommandText =
                            @"
                                INSERT INTO student (id,class_id,name,no,gender)
                                VALUES ($id,$class_id,$name,$no,$gender)
                            ";

                            command.Parameters.AddWithValue("$id", student.id);
                            command.Parameters.AddWithValue("$class_id", student.class_id);
                            command.Parameters.AddWithValue("$name", student.name);
                            command.Parameters.AddWithValue("$no", student.no);
                            command.Parameters.AddWithValue("$gender", student.gender);

                            command.ExecuteNonQuery();

                        }
                    }

                    trans.Commit();
                }


            }

            return new UpdateClassResponse(){
                status = ResponseStatus.sussess,
                data = data
            };
        }
    }

    public class Class{
        public String id {get;set;}
        public String name {get;set;}
        public Student[] students {get;set;}
    }

    public class Student{
        public String id {get;set;}
        public String class_id {get;set;}
        public String name {get;set;}
        public String no {get;set;}
        public String gender {get;set;}
    }

    public class Request<T>{
        public T data {get;set;}
    }

    public class Response<T>{
        public ResponseStatus status {get;set;}
        public String message {get;set;}
        public T data {get;set;}
    }

    public enum ResponseStatus{
        sussess,
        fail
    }

    public class CreateClassRequest : Request<Class>{

    }

    public class CreateClassResponse : Response<Class>{
        
    }

    public class UpdateClassRequest : Request<Class>{
        
    }

    public class UpdateClassResponse : Response<Class>{
        
    }

    public class GetClassesResponse : Response<Class[]>{
        
    }
}
