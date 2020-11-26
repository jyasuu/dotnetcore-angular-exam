import { Component, OnInit,Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-class-manager',
  templateUrl: './class-manager.component.html',
  styleUrls: ['./class-manager.component.css']
})
export class ClassManagerComponent implements OnInit {
  public classes: Class[];
  public editingClass: Class;

  http: HttpClient;
  baseUrl: string;

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;


  }

  public editClass(editClass: Class) {
    var temp: Class = JSON.parse(JSON.stringify(editClass));
    this.editingClass = temp;

  }

  public newClass() {
    this.editingClass = { students: [] };

  }

  public saveClass(): void {

    if(this.editingClass.id){

      var req : UpdateClassRequest = {data : this.editingClass}
      this.http.put<UpdateClassResponse>(this.baseUrl + 'ClassManager',req).subscribe(result => {

        var temp = this.classes.find(a=> a.id === result.data.id);
        temp.name = result.data.name;
        temp.students = result.data.students;
        this.editingClass = null;

      }, error => console.error(error));

    }
    else{
      var req : CreateClassRequest = {data : this.editingClass}
      this.http.post<CreateClassResponse>(this.baseUrl + 'ClassManager',req).subscribe(result => {
        this.classes.push(result.data);
        this.editingClass = null;
      }, error => console.error(error));
    }



  }

  public cancelEditClass() {
    this.editingClass = null;
  }

  public deleteStudent(student: Student) {
    this.editingClass.students = this.editingClass.students.filter(a => a !== student);

  }

  public newStudent() {
    if(!this.editingClass.students)
      this.editingClass.students = [];
    this.editingClass.students.push({});

  }

  ngOnInit(): void {

    this.http.get<GetClassResponse>(this.baseUrl + 'ClassManager').subscribe(result => {
      this.classes = result.data;
    }, error => console.error(error));

  }

}

interface Request<T>{
  data: T;
}

interface Response<T>{
  data: T;
}

interface CreateClassRequest extends Request<Class>{
}

interface CreateClassResponse extends Response<Class>{
}

interface UpdateClassRequest extends Request<Class>{
}

interface UpdateClassResponse extends Response<Class>{
}

interface GetClassResponse{
  data: Class[];
}


interface Class {
  id?: number;
  name?: string;
  students?: Student[];

}

interface Student {
  name?: string;
  no?: string;
  gender?: string;
}
