using System;
using System.Collections.Generic;
using System.Web;
using System.Xml.Serialization;
using System.Linq;

namespace UGTUKadrService
{


    /// <summary>
    /// Класс с описанием отдела
    /// </summary>
    /*[Serializable]
    public class UGTUDepartmentInfoDTO
    {
        /// <summary>
        /// Получает или устанавливает отдел
        /// </summary>
        public Department Department { get; set; }

    }*/

    /// <summary>
    /// Класс с описанием отдела
    /// </summary>
    [Serializable]
    public class Department
    {
        /// <summary>
        /// Получает или устанавливает код отдела
        /// </summary>
        public int idDepartment { get; set; }

        /// <summary>
        /// Получает или устанавливает название должности
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Получает или устанавливает краткое название должности
        /// </summary>
        public string DepartmentSmallName { get; set; }

        /// <summary>
        /// Руководящий отдел 
        /// </summary>
        //public Department ManagerDepartment { get; set; }

        /// <summary>
        /// Получает или устанавливает список должностей отдела
        /// </summary>
        public Post[] Posts { get; set; }
        /// <summary>
        /// Получает или устанавливает список дочерних отделов для отдела
        /// </summary>
        public Department[] SubDepartments { get; set; }


    }


    /// <summary>
    /// Класс с описанием отдела (плоский)
    /// </summary>
    [Serializable]
    public class DepartmentView
    {
        /// <summary>
        /// Получает или устанавливает код отдела
        /// </summary>
        public int idDepartment { get; set; }

        /// <summary>
        /// Получает или устанавливает название должности
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Получает или устанавливает краткое название должности
        /// </summary>
        public string DepartmentSmallName { get; set; }

        /// <summary>
        /// Руководящий отдел 
        /// </summary>
        //public Department ManagerDepartment { get; set; }


        /// <summary>
        /// Код родительского отдела
        /// </summary>
        public int? idManagerDepartment { get; set; }

        /// <summary>
        /// Название родительского отдела
        /// </summary>
        public string ManagerDepartmentName { get; set; }

        /// <summary>
        /// Краткое название родительского отдела
        /// </summary>
        public string ManagerDepartmentSmallName { get; set; }

        /// <summary>
        /// Получает или устанавливает список должностей отдела
        /// </summary>
        public Post[] Posts { get; set; }
    }


    /// <summary>
    /// Класс описания должности
    /// </summary>
    [Serializable]
    public class Post
    {
        /// <summary>
        /// Получает или устанавливает идентификатор отдела
        /// </summary>
        public int idDepartment { get; set; }
        
        /// <summary>
        /// Получает или устанавливает идентификатор должности
        /// </summary>
        public int idPost { get; set; }

        /// <summary>
        /// Получает или устанавливает название должности
        /// </summary>        
        public string PostName { get; set;}

       /// <summary>
        /// Получает или устанавливает название краткое должности
        /// </summary>        
        public string PostShortName {get; set;}

        /// <summary>
        /// Получает или устанавливает кол-во ставок 
        /// </summary>
        public decimal StaffCount {get; set;}

        /// <summary>
        /// Получает или устанавливает признак руководителя
        /// </summary>
        public bool ManagerBit { get; set; }
    }

    /// <summary>
    /// Класс описания должности
    /// </summary>
    [Serializable]
    public class Employee
    {
        /// <summary>
        /// Получает или устанавливает идентификатор сотрудника
        /// </summary>
        public int idEmployee { get; set; }

        /// <summary>
        /// ФИО сотрудника
        /// </summary>
        public string EmployeeFullName { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string EmployeeUserName { get; set; }

        /// <summary>
        /// Получает или устанавливает штата
        /// </summary>
        public int idFactStaff { get; set; }

        /// <summary>
        /// Получает или устанавливает идентификатор должности
        /// </summary>
        public int idPost { get; set; }

        /// <summary>
        /// Получает или устанавливает название должности
        /// </summary>        
        public string PostName { get; set; }

        /// <summary>
        /// Получает или устанавливает кол-во ставок 
        /// </summary>
        public decimal StaffCount { get; set; }

        /// <summary>
        /// Получает или устанавливает признак руководителя
        /// </summary>
        public bool ManagerBit { get; set; }
    }
}