using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using UGTUKadrService.Data;

namespace UGTUKadrService
{
    /// <summary>
    /// Summary description for KadrService
    /// </summary>
    [WebService(Namespace = "http://services.ist.ugtu.net/", Name = "Сервис по штатному расписанию УГТУ")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
   
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class KadrService : System.Web.Services.WebService
    {
        /// <summary>
        /// Возвращает ключ корневого отдела (родительский - т.е. УГТУ)
        /// </summary>
        /// <returns></returns>
        public int GetRootDepartmentID(DBKadrDataContext ctx)
        {
            Data.Department department = ctx.Departments.Where(dep => dep.Department1 == null).FirstOrDefault();
            if (department != null)
                return department.id;
            else
                return -1;
        }


        /*/// <summary>
        /// Рекурсивная функция. Возвращает дерево подчиненных отделов (иерархию).
        /// </summary>
        /// <param name="ctx">Контекст</param>
        /// <param name="idDepartment">Код отдела, для которого нужно вернуть подчиненных</param>
        /// <returns></returns>
        public Department[] GetSubDepartments(DBKadrDataContext ctx, Department rootDepartment)
        {
            //получаем непосредственных подчиненных
            Department[] SubDeps = ctx.Departments.Where(dep => dep.Department1.id == rootDepartment.idDepartment).Select(dep =>
                new Department()
                {
                    idDepartment = dep.id,
                    DepartmentName = dep.DepartmentName,
                    DepartmentSmallName = dep.DepartmentSmallName
                }).ToArray();

            //получаем должности
            IEnumerable<Data.GetPlanStaffByPeriodResult> planStaff = ctx.GetPlanStaffByPeriod(DateTime.Now, DateTime.Now).Where(plSt =>
                plSt.idDepartment == rootDepartment.idDepartment).ToArray();

            rootDepartment.Posts = planStaff.Join(ctx.Posts, plSt => plSt.idPost, post => post.id, (plSt, post) =>
                new Post() { idDepartment = Convert.ToInt32(plSt.idDepartment), idPost = post.id, ManagerBit = post.ManagerBit, PostName = post.PostName, PostShortName = post.PostShortName, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();


            //для каждого из них получаем подчиненных
            foreach (Department subDep in SubDeps)
            {
                subDep.SubDepartments = GetSubDepartments(ctx, subDep);
            }
            return SubDeps;
        }

        [WebMethod(Description="При передаче -1, возвращает все отделы университета в виде дерева, иначе возвращает отделы заданного отдела") ]
        public Department GetDepartmentsTree(int idDepartment)
        {
            using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
            {
                //если отдел не указан, определяем для родительского отдела
                if (idDepartment == -1)
                {
                    idDepartment = GetRootDepartmentID(ctx);
                }

                //поиск отдела
                Department rootDepartment = ctx.Departments.Where(dep => dep.id == idDepartment).Select(dep =>
                new Department()
                {
                    idDepartment = dep.id,
                    DepartmentName = dep.DepartmentName,
                    DepartmentSmallName = dep.DepartmentSmallName
                }).FirstOrDefault();
                if (rootDepartment == null)
                    return null;

                //поиск подчиненных отделов
                rootDepartment.SubDepartments = GetSubDepartments(ctx, rootDepartment);

                return rootDepartment;
            }

        }*/


        [WebMethod(Description = "При передаче -1, возвращает все отделы университета, иначе возвращает отделы заданного отдела в табличной форме", CacheDuration=21600)]
        public DepartmentView[] GetDepartmentsList(int idDepartment)
        {
            using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
            {
                //если отдел не указан, определяем для родительского отдела
                if (idDepartment == -1)
                {
                    idDepartment = GetRootDepartmentID(ctx);
                }

                //получаем все отделы
                DepartmentView[] DepartmentList = ctx.GetSubDepartments(idDepartment).Join(ctx.Departments.Where(dep => (dep.dateExit > DateTime.Today) || (dep.dateExit == null)), subDep => subDep.idDepartment, dep => dep.id, (subDep, dep) => 
                    new { idDepartment = dep.id, DepartmentName = dep.DepartmentName, DepartmentSmallName = dep.DepartmentSmallName, 
                        idManagerDepartment = dep.idManagerDepartment }).Join(ctx.Departments, subDep => subDep.idManagerDepartment,
                        dep => dep.id, (subDep, dep) => 
                            new DepartmentView() 
                            {
                                idDepartment = subDep.idDepartment,
                                DepartmentName = subDep.DepartmentName,
                                DepartmentSmallName = subDep.DepartmentSmallName,
                                idManagerDepartment = subDep.idManagerDepartment,
                                ManagerDepartmentName = dep.DepartmentName,
                                ManagerDepartmentSmallName = dep.DepartmentSmallName
                            }).ToArray();

                return DepartmentList;
            }

        }


        [WebMethod(Description = "Bозвращает все отделы университета в табличной форме", CacheDuration = 21600)]
        public DepartmentView[] GetFullDepartmentsList()
        {
            return GetDepartmentsList(-1);
        }



       [WebMethod(Description = "Возвращает информацию по заданному отделу, включая должности", CacheDuration = 21600)]
        public DepartmentView GetDepartmentsInfo(int idDepartment)
        {
            using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
            {
                //если отдел не указан
                if (idDepartment < 0)
                {
                    return null;
                }

                //получаем отдел
                DepartmentView DepartmentInfo = ctx.Departments.Where(dep => dep.id == idDepartment).Select(dep =>
                    new DepartmentView()
                    {
                        idDepartment = dep.id,
                        DepartmentName = dep.DepartmentName,
                        DepartmentSmallName = dep.DepartmentSmallName,
                        idManagerDepartment = dep.idManagerDepartment,
                        ManagerDepartmentName = dep.Department1.DepartmentName,
                        ManagerDepartmentSmallName = dep.Department1.DepartmentSmallName
                    }).FirstOrDefault();
                    
                //получаем список должностей отдела
                var planStaff = ctx.GetPlanStaffByPeriod(DateTime.Now, DateTime.Now).Where(plSt => plSt.idDepartment == idDepartment).ToArray();

                //заполняем должности отделa
                Post[] Posts = planStaff.Join(ctx.Posts, plSt => plSt.idPost, post => post.id, (plSt, post) =>
                    new Post() { idDepartment = Convert.ToInt32(plSt.idDepartment), idPost = post.id, ManagerBit = post.ManagerBit, PostName = post.PostName, PostShortName = post.PostShortName, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();
                DepartmentInfo.Posts = Posts;

                return DepartmentInfo;
            }

        }


       [WebMethod(Description = "Возвращает список всех должностей", CacheDuration = 21600)]
       public Post[] GetPostsList()
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               int idDepartment = GetRootDepartmentID(ctx);

               //получаем список всех отделов (чтобы исключить тестовые)
               IEnumerable<GetSubDepartmentsResult> deps = ctx.GetSubDepartments(idDepartment);

               //получаем все записи штатного расписания отделов
               var planStaff = ctx.GetPlanStaffByPeriod(DateTime.Now, DateTime.Now).Join(deps, plSt => plSt.idDepartment,
                   dep => dep.idDepartment, (plSt, dep) => new { idPost = plSt.idPost, idDepartment = plSt.idDepartment, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();
                
               //заполняем должности отделов
               Post[] Posts = planStaff.Join(ctx.Posts, plSt => plSt.idPost, post => post.id, (plSt, post) =>
                   new Post() { idDepartment = Convert.ToInt32(plSt.idDepartment), idPost = post.id, ManagerBit = post.ManagerBit, PostName = post.PostName, PostShortName = post.PostShortName, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();

               return Posts;
           }

       }

       [WebMethod(Description = "Возвращает информацию по заданной должности", CacheDuration = 21600)]
       public Post[] GetPostInfo(int idPost)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               int idDepartment = GetRootDepartmentID(ctx);

               //получаем список всех отделов (чтобы исключить тестовые)
               IEnumerable<GetSubDepartmentsResult> deps = ctx.GetSubDepartments(idDepartment);

               //получаем все записи штатного расписания отделов
               var planStaff = ctx.GetPlanStaffByPeriod(DateTime.Now, DateTime.Now).Where(plSt => plSt.idPost == idPost).Join(deps, plSt => plSt.idDepartment,
                   dep => dep.idDepartment, (plSt, dep) => new { idPost = plSt.idPost, idDepartment = plSt.idDepartment, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();

               //заполняем должности отделов
               Post[] Posts = planStaff.Join(ctx.Posts, plSt => plSt.idPost, post => post.id, (plSt, post) =>
                   new Post() { idDepartment = Convert.ToInt32(plSt.idDepartment), idPost = post.id, ManagerBit = post.ManagerBit, PostName = post.PostName, PostShortName = post.PostShortName, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();

               return Posts;
           }

       }

       [WebMethod(Description = "Возвращает список всех сотрудников, занимающих указанную должность в заданном отделе", CacheDuration = 21600)]
       public Employee[] GetEmployees(int idPost, int idDepartment)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если отдел не указан, определяем для родительского отдела
               if (idDepartment < 0)
               {
                   return null;
               }

               //получаем всех сотрудников отдела на заданной должности
               Data.GetDepartmentStaffResult[] staff = ctx.GetDepartmentStaff(idDepartment, DateTime.Now, DateTime.Now, false).Where(st => (st.idPost == idPost) && (st.idEmployee != null)).ToArray();
               if (staff.Count() > 0)
               {
                   Employee[] employees = staff.Select(st
                       => new Employee
                       {
                           idEmployee = Convert.ToInt32(st.idEmployee),
                           EmployeeFullName = st.EmplFullName,
                           EmployeeUserName = Convert.ToString(st.EmployeeLogin),
                           idPost = Convert.ToInt32(st.idPost),
                           PostName = st.PostName,
                           StaffCount = Convert.ToDecimal(st.factStaffCount),
                           ManagerBit = Convert.ToBoolean(st.ManagerBit),
                           idFactStaff = Convert.ToInt32(st.idFactStaff)
                       }).ToArray();
                    return employees;
               }
               else
                   return null;

           }

       }

       [WebMethod(Description = "Возвращает список всех отделов, в которых работает указанный сотрудник по логину. Не указывается руководящий отдел.", CacheDuration = 21600)]
       public DepartmentView[] GetDepartmentsByUserName(string userName)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               int idEmployee = ctx.Employees.Where(empl => empl.EmployeeLogin == userName).FirstOrDefault().id;
               return GetEmployeesDepartments(idEmployee);
           }
       }

       [WebMethod(Description = "Возвращает список всех отделов, в которых работает указанный сотрудник. Не указывается руководящий отдел.", CacheDuration = 21600)]
       public DepartmentView[] GetEmployeesDepartments(int idEmployee)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если сотрудник не указан, завершение
               if (idEmployee < 0)
               {
                   return null;
               }

               //получаем список должностей
               var factStaff = ctx.GetFactStaffByPeriod(DateTime.Now, DateTime.Now).Where(fcSt => fcSt.idEmployee == idEmployee).ToArray().Join(ctx.PlanStaffs, 
                   fcSt => fcSt.idPlanStaff, plSt => plSt.id, (fcSt, plSt) =>
                    new {idDepartment = plSt.idDepartment}).Distinct().ToArray();

               //получаем всех сотрудников отдела на заданной должности
               DepartmentView[] departments = factStaff.Join(ctx.Departments, fcSt => fcSt.idDepartment, dep => dep.id,
                   (fcSt, dep) => new DepartmentView
                   {
                       idDepartment = dep.id,
                       DepartmentName = dep.DepartmentName,
                       DepartmentSmallName = dep.DepartmentSmallName,
                       idManagerDepartment = dep.idManagerDepartment,
                       ManagerDepartmentName = ' '.ToString(),
                       ManagerDepartmentSmallName = ' '.ToString()
                   }).ToArray();

               return departments;
           }

       }

       [WebMethod(Description = "Возвращает ИО (замещение) сотрудника.", CacheDuration = 21600)]
       public Employee[] GetReplacements(int idFactStaff)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если сотрудник не указан, завершение
               if (idFactStaff < 0)
               {
                   return null;
               }

               int idDepartment = Convert.ToInt32(ctx.FactStaffs.Where(fcSt => fcSt.id == idFactStaff).Select(fcSt => fcSt.PlanStaff.idDepartment).FirstOrDefault());
 
               //получаем все замещающих заданного сотрудника
               Data.FactStaffReplacement[] replacmentFactStaff = ctx.FactStaffReplacements.Where(repl => repl.idReplacedFactStaff == idFactStaff).ToArray();

               if ((replacmentFactStaff.Count() > 0) && (idDepartment > 0))
               {
                   Employee[] replacments = ctx.GetDepartmentStaff(idDepartment, DateTime.Now, DateTime.Now, false).ToArray().Join(replacmentFactStaff, st => st.idFactStaff, repl => repl.idFactStaff,
                       (st, repl) => new Employee
                       {
                           idEmployee = Convert.ToInt32(st.idEmployee),
                           EmployeeFullName = st.EmplFullName,
                           EmployeeUserName = Convert.ToString(st.EmployeeLogin),
                           idPost = Convert.ToInt32(st.idPost),
                           PostName = st.PostName,
                           StaffCount = Convert.ToDecimal(st.factStaffCount),
                           ManagerBit = Convert.ToBoolean(st.ManagerBit),
                           idFactStaff = Convert.ToInt32(st.idFactStaff)
                       }).ToArray();
                   return replacments;
               }
               else
                    return null;
           }

       }

       [WebMethod(Description = "Возвращает сотрудника по idFactStaff.", CacheDuration = 21600)]
       public Employee GetEmployee(int idFactStaff)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если сотрудник не указан, завершение
               if (idFactStaff < 0)
               {
                   return null;
               }

               Employee employee = ctx.FactStaffs.Where(fcSt => fcSt.id == idFactStaff).Join(ctx.Posts, fcSt => fcSt.PlanStaff.idPost, post => post.id,
                   (fcSt, post) => new
                   {
                       idEmployee = fcSt.idEmployee,
                       //idFactStaff = fcSt.id,
                       idPost = post.id,
                       PostName = post.PostName,
                       ManagerBit = post.ManagerBit
                   }).ToArray().Join(ctx.Employees, fcSt => fcSt.idEmployee, empl => empl.id,
                    (fcSt, empl) => new Employee
                    {
                        idEmployee = fcSt.idEmployee,
                        EmployeeFullName = empl.LastName+ " "+empl.FirstName + " " + empl.Otch,
                        idPost = fcSt.idPost,
                        PostName = fcSt.PostName,
                        StaffCount = 0,
                        ManagerBit = fcSt.ManagerBit,
                        idFactStaff = idFactStaff,
                        EmployeeUserName = Convert.ToString(empl.EmployeeLogin)
                    }).FirstOrDefault();

               return employee;
           }

       }


       [WebMethod(Description = "Возвращает сотрудников отдела по дню рождения.", CacheDuration = 21600)]
       public Employee GetNearesEmployeeBirthDates(int idDepartment, string period)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если сотрудник не указан, завершение
               if (idFactStaff < 0)
               {
                   return null;
               }

               Employee employee = ctx.FactStaffs.Where(fcSt => fcSt.id == idFactStaff).Join(ctx.Posts, fcSt => fcSt.PlanStaff.idPost, post => post.id,
                   (fcSt, post) => new
                   {
                       idEmployee = fcSt.idEmployee,
                       //idFactStaff = fcSt.id,
                       idPost = post.id,
                       PostName = post.PostName,
                       ManagerBit = post.ManagerBit
                   }).ToArray().Join(ctx.Employees, fcSt => fcSt.idEmployee, empl => empl.id,
                    (fcSt, empl) => new Employee
                    {
                        idEmployee = fcSt.idEmployee,
                        EmployeeFullName = empl.LastName + " " + empl.FirstName + " " + empl.Otch,
                        idPost = fcSt.idPost,
                        PostName = fcSt.PostName,
                        StaffCount = 0,
                        ManagerBit = fcSt.ManagerBit,
                        idFactStaff = idFactStaff,
                        EmployeeUserName = Convert.ToString(empl.EmployeeLogin)
                    }).FirstOrDefault();
               return ctx.Persons.Where(p => (p.Dd_birth.HasValue) &&
    (p.Dd_birth.GetValueOrDefault().Day == DateTime.Today.Day) &&
    (p.Dd_birth.GetValueOrDefault().Month == DateTime.Today.Month)).Select(x =>
        new PersonBirthDateDTO()
        {
            BirthDate = x.Dd_birth.GetValueOrDefault(),
            FamilyName = x.Clastname,
            FirstName = x.Cfirstname,
            MiddleName = x.Cotch,
            EMail = x.cEmail,
            DayMonth = string.Format("{0}.{1}", x.Dd_birth.GetValueOrDefault().Day, x.Dd_birth.GetValueOrDefault().Month)
        }).ToArray();                                  

               return employee;
           }

       }
 
    }
}