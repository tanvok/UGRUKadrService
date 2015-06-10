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
        private Data.GetDepartmentStaffResult[] staff;
        
        /// <summary>
        /// Возвращает ключ корневого отдела (родительский - т.е. УГТУ)
        /// </summary>
        /// <returns></returns>
        public int GetRootDepartmentID(DBKadrDataContext ctx)
        {
            Data.Department department = ctx.Departments.Where(dep => dep.idManagerDepartment == null).FirstOrDefault();
            if (department != null)
                return department.id;
            else
                return -1;
        }


        /// <summary>
        /// Рекурсивная функция. Возвращает дерево подчиненных отделов (иерархию).
        /// </summary>
        /// <param name="ctx">Контекст</param>
        /// <param name="idDepartment">Код отдела, для которого нужно вернуть подчиненных</param>
        /// <returns></returns>
        public Department[] GetSubDepartments(DBKadrDataContext ctx, GetDepartmentsForPeriodResult[] Departments, Department rootDepartment)
        {
            //получаем непосредственных подчиненных
            Department[] SubDeps = Departments.Where(dep => dep.idManagerDepartment == rootDepartment.idDepartment).Select(dep =>
                new Department()
                {
                    idDepartment = dep.idDepartment,
                    DepartmentName = dep.DepartmentName,
                    DepartmentSmallName = dep.DepartmentSmallName
                }).ToArray();

            //получаем должности
            IEnumerable<Data.GetPlanStaffByPeriodResult> planStaff = ctx.GetPlanStaffByPeriod(DateTime.Now, DateTime.Now).Where(plSt =>
                plSt.idDepartment == rootDepartment.idDepartment).ToArray();

            rootDepartment.Posts = planStaff.Join(ctx.Posts, plSt => plSt.idPost, post => post.id, (plSt, post) =>
                new Post() { idDepartment = Convert.ToInt32(rootDepartment.idDepartment), idPost = post.id, ManagerBit = post.ManagerBit, PostName = post.PostName, PostShortName = post.PostShortName, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();

            if (staff != null)
            {
                //для каждого из них получаем подчиненных
                foreach (Post post in rootDepartment.Posts)
                {
                    post.Employees = staff.Where(st => (st.idPost == post.idPost) && (st.idEmployee != null) && (st.idDepartment == rootDepartment.idDepartment)).Select(st
                            => new Employee
                            {
                                idEmployee = Convert.ToInt32(st.idEmployee),
                                EmployeeFullName = st.EmplFullName,
                                EmployeeUserName = Convert.ToString(st.EmployeeLogin),
                                idPost = Convert.ToInt32(st.idPost),
                                PostName = st.PostName,
                                StaffCount = Convert.ToDecimal(st.factStaffCount),
                                ManagerBit = Convert.ToBoolean(st.ManagerBit),
                                idFactStaff = Convert.ToInt32(st.idFactStaff),
                                idPlanStaff = (int)st.idPlanStaff
                            }).ToArray();
                }
            }

            //для каждого из них получаем подчиненных
            foreach (Department subDep in SubDeps)
            {
                subDep.SubDepartments = GetSubDepartments(ctx, Departments, subDep);
            }
            return SubDeps;
        }

        [WebMethod(Description="При передаче -1, возвращает все отделы университета в виде дерева, иначе возвращает отделы заданного отдела. В данных по отделу идут должности и сотрудники." , CacheDuration = 21600)]
        public Department GetDepartmentsTree(int idDepartment)
        {
            using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
            {
                //если отдел не указан, определяем для родительского отдела
                if (idDepartment == -1)
                {
                    idDepartment = GetRootDepartmentID(ctx);
                }

                //получаем все текущие отделы
                GetDepartmentsForPeriodResult[] Departments = ctx.GetDepartmentsForPeriod(DateTime.Now, DateTime.Now).ToArray();

                //поиск отдела
                Department rootDepartment = Departments.Where(dep => dep.idDepartment == idDepartment).Select(dep =>
                new Department()
                {
                    idDepartment = dep.idDepartment,
                    DepartmentName = dep.DepartmentName,
                    DepartmentSmallName = dep.DepartmentSmallName
                }).FirstOrDefault();
                if (rootDepartment == null)
                    return null;

                //получаем всех сотрудников отдела на заданной должности
                staff = ctx.GetDepartmentStaff(idDepartment, DateTime.Now, DateTime.Now, true).ToArray();
 
                //поиск подчиненных отделов
                rootDepartment.SubDepartments = GetSubDepartments(ctx, Departments, rootDepartment);

                return rootDepartment;
            }

        }


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
                        ManagerDepartmentName = ctx.Departments.Where(d => d.id == dep.idManagerDepartment).Select(d => d.DepartmentName).FirstOrDefault(),
                        ManagerDepartmentSmallName = ctx.Departments.Where(d => d.id == dep.idManagerDepartment).Select(d => d.DepartmentSmallName).FirstOrDefault()
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

       /*[WebMethod(Description = "Возвращает дерево по заданному отделу, включая должности", CacheDuration = 21600)]
       public DepartmentView GetDepartmentsTree(int idDepartment)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если отдел не указан, определяем для родительского отдела
               if (idDepartment == -1)
               {
                   idDepartment = GetRootDepartmentID(ctx);
               }

               //если отдел не указан
               if (idDepartment < 0)
               {
                   return null;
               }

               //получаем информацию по самому отделу
               DepartmentView DepartmentInfo = ctx.Departments.Where(dep => dep.id == idDepartment).Select(dep =>
                   new DepartmentView()
                   {
                       idDepartment = dep.id,
                       DepartmentName = dep.DepartmentName,
                       DepartmentSmallName = dep.DepartmentSmallName,
                       idManagerDepartment = dep.idManagerDepartment,
                       ManagerDepartmentName = ctx.Departments.Where(d => d.id == dep.idManagerDepartment).Select(d => d.DepartmentName).FirstOrDefault(),
                       ManagerDepartmentSmallName = ctx.Departments.Where(d => d.id == dep.idManagerDepartment).Select(d => d.DepartmentSmallName).FirstOrDefault()
                   }).FirstOrDefault();

               //получаем список должностей отдела
               var planStaff = ctx.GetPlanStaffByPeriod(DateTime.Now, DateTime.Now).Where(plSt => plSt.idDepartment == idDepartment).ToArray();

               //заполняем должности отделa
               Post[] Posts = planStaff.Join(ctx.Posts, plSt => plSt.idPost, post => post.id, (plSt, post) =>
                   new Post() { idDepartment = Convert.ToInt32(plSt.idDepartment), idPost = post.id, ManagerBit = post.ManagerBit, PostName = post.PostName, PostShortName = post.PostShortName, StaffCount = Convert.ToDecimal(plSt.StaffCount) }).ToArray();
               DepartmentInfo.Posts = Posts;

               return DepartmentInfo;
           }

       }*/

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
                           idFactStaff = Convert.ToInt32(st.idFactStaff),
                           idPlanStaff = (int)st.idPlanStaff
                       }).ToArray();
                    return employees;
               }
               else
                   return null;

           }

       }

       [WebMethod(Description = "Возвращает список всех сотрудников, работающих в заданном отделе. При передаче -1, возвращает для всех отделов университета", CacheDuration = 21600)]
       public Employee[] GetAllEmployees(int idDepartment)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               bool WithSubDeps = false;
               //если отдел не указан, определяем для родительского отдела
               if (idDepartment == -1)
               {
                   idDepartment = GetRootDepartmentID(ctx);
                   WithSubDeps = true;
               }
               
               //если отдел не указан, определяем для родительского отдела
               if (idDepartment < 0)
               {
                   return null;
               }

               //получаем всех сотрудников отдела на заданной должности
               Data.GetDepartmentStaffResult[] staff = ctx.GetDepartmentStaff(idDepartment, DateTime.Now, DateTime.Now, WithSubDeps).Where(st => (st.idEmployee != null)).ToArray();
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
                           idFactStaff = Convert.ToInt32(st.idFactStaff),
                           idDepartment = st.idDepartment,
                           idPlanStaff = (int)st.idPlanStaff,
                           TabNumber = st.itab_n
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
               var employee = ctx.Employees.Where(empl => empl.EmployeeLogin == userName).FirstOrDefault();
               if (employee != null)
                    return GetEmployeesDepartments(employee.id);
               return null;
           }
       }

       [WebMethod(Description = "Возвращает список всех отделов, в которых работает указанный сотрудник по ФИО. ФИО можно указывать не полностью", CacheDuration = 21600)]
       public Employee[] GetDepartmentsByEmployeeName(string LastName, string FirstName, string Otch)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               Employee[] employees;
               //если отдел не указан, определяем для родительского отдела
               int idDepartment = GetRootDepartmentID(ctx);
               employees =
                   ctx.GetStaffByPeriod(DateTime.Today.Date, DateTime.Today.Date).Join(ctx.Employees.Where(empl => empl.LastName.Contains(LastName)).Where(empl => empl.FirstName.Contains(FirstName)).Where(empl => empl.Otch.Contains(Otch))//.Where(empl => empl.BirthDate != null)
                       , fcSt => fcSt.idEmployee.Value, empl => empl.id,
                       (fcSt, empl) => new Employee
                       {
                           idEmployee = empl.id,
                           EmployeeFullName = empl.LastName + " " + empl.FirstName + " " + empl.Otch,
                           idPost =fcSt.idPost.Value ,
                           //PostName = null,
                           //StaffCount = 0,
                           //idFactStaff = fcSt.idFactStaff.Value,
                           EmployeeUserName = Convert.ToString(empl.EmployeeLogin),
                           BirthYear = ((empl.BirthDate.Value == null ? DateTime.Today : empl.BirthDate.Value).Year == DateTime.Today.Year) ? -1 : (empl.BirthDate.Value == null ? DateTime.Today : empl.BirthDate.Value).Year,
                           idDepartment = fcSt.idDepartment
                       }).Join(ctx.Departments, empl => empl.idDepartment, dep => dep.id,
                        (empl, dep) => new Employee
                        {
                            idEmployee = empl.idEmployee,
                            EmployeeFullName = empl.EmployeeFullName,
                            idPost = empl.idPost,
                            //PostName = null,
                            //StaffCount = 0,
                            //idFactStaff = fcSt.idFactStaff.Value,
                            EmployeeUserName = empl.EmployeeUserName,
                            BirthYear = empl.BirthYear,
                            idDepartment = empl.idDepartment,
                            DepartmentName = dep.DepartmentName
                        }).Join(ctx.Posts, empl => empl.idPost, post => post.id,
                        (empl, post) => new Employee
                        {
                            idEmployee = empl.idEmployee,
                            EmployeeFullName = empl.EmployeeFullName,
                            idPost = empl.idPost,
                            PostName = post.PostName,
                            //StaffCount = 0,
                            //idFactStaff = fcSt.idFactStaff.Value,
                            EmployeeUserName = empl.EmployeeUserName,
                            BirthYear = empl.BirthYear,
                            idDepartment = empl.idDepartment,
                            DepartmentName = empl.DepartmentName
                        }).Distinct().ToArray();
               return employees;
           }
               
        }

       [WebMethod(Description = "Возвращает список должностей, на которых работает указанный сотрудник по логину. Не указывается руководящий отдел.", CacheDuration = 21600)]
       public Employee[] GetEmployeeByUserName(string userName)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               var employee = ctx.Employees.Where(empl => empl.EmployeeLogin == userName).FirstOrDefault();
               if (employee != null)
                   return GetAllEmployees(-1).Where(empl => empl.idEmployee == employee.id).ToArray();
               return null;
           }
       }


       [WebMethod(Description = "Возвращает список всех отделов, в которых работает указанный сотрудник. Не указывается руководящий отдел.", CacheDuration = 21600)]
       public DepartmentView[] GetEmployeesDepartments(int idEmployee)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               //если сотрудник не указан, завершение
               if (idEmployee <= 0)
               {
                   return null;
               }

               //получаем всех сотрудников отдела на заданной должности
               DepartmentView[] departments = ctx.GetFactStaffByPeriod(DateTime.Today.Date, DateTime.Today.Date).Where(fcSt => fcSt.idEmployee.Value == idEmployee).ToArray().Join(ctx.PlanStaffs,
                   fcSt => fcSt.idPlanStaff.Value, plSt => plSt.id, (fcSt, plSt) =>
                    new { idDepartment = plSt.idDepartment}).ToArray().Join(ctx.Departments, fcSt => fcSt.idDepartment, dep => dep.id,
                   (fcSt, dep) => new DepartmentView
                   {
                       idDepartment = dep.id,
                       DepartmentName = dep.DepartmentName,
                       DepartmentSmallName = dep.DepartmentSmallName,
                       idManagerDepartment = dep.idManagerDepartment,
                       ManagerDepartmentName = "",
                       ManagerDepartmentSmallName = ""
                   }).Distinct().ToArray();
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
               Data.FactStaffReplacement[] replacementFactStaff = ctx.FactStaffReplacements.Where(repl => (repl.idReplacedFactStaff == idFactStaff) && ((repl.DateEnd == null) || (repl.DateEnd.GetValueOrDefault().Day >= DateTime.Today.Day))).ToArray();

               if ((replacementFactStaff.Count() > 0) && (idDepartment > 0))
               {
                   Employee[] replacements = ctx.GetDepartmentStaff(idDepartment, DateTime.Now, DateTime.Now, false).ToArray().Join(replacementFactStaff, st => st.idFactStaff, repl => repl.idFactStaff,
                       (st, repl) => new Employee
                       {
                           idEmployee = Convert.ToInt32(st.idEmployee),
                           EmployeeFullName = st.EmplFullName,
                           EmployeeUserName = Convert.ToString(st.EmployeeLogin),
                           idPost = Convert.ToInt32(st.idPost),
                           PostName = st.PostName,
                           StaffCount = Convert.ToDecimal(st.factStaffCount),
                           ManagerBit = Convert.ToBoolean(st.ManagerBit),
                           idFactStaff = Convert.ToInt32(st.idFactStaff),
                           idPlanStaff = (int)st.idPlanStaff
                       }).ToArray();
                   return replacements;
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
                       ManagerBit = post.ManagerBit,
                       idPlanStaff = fcSt.idPlanStaff
                   }).ToArray().Join(ctx.Employees, fcSt => fcSt.idEmployee, empl => empl.id,
                    (fcSt, empl) => new Employee
                    {
                        idEmployee = fcSt.idEmployee,
                        EmployeeFullName = empl.LastName+ " "+empl.FirstName + " " + empl.Otch,
                        idPost = /*fcSt.idPost*/1,
                        PostName = /*fcSt.PostName*/"",
                        StaffCount = 0,
                        ManagerBit = /*fcSt.ManagerBit*/false,
                        idFactStaff = idFactStaff,
                        EmployeeUserName = Convert.ToString(empl.EmployeeLogin),
                        idPlanStaff = fcSt.idPlanStaff
                    }).FirstOrDefault();

               return employee;
           }

       }


       [WebMethod(Description = "Возвращает сотрудников отдела по дню рождения.", CacheDuration = 21600)]
       public Employee[] GetNearesEmployeeBirthDates(int idDepartment, DateTime date)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               Employee[] employees;
               //если отдел не указан, определяем для родительского отдела
               if (idDepartment == -1)
               {
                   idDepartment = GetRootDepartmentID(ctx);
                   employees =
                       ctx.GetStaffByPeriod(date, date).Join(ctx.Employees.Where(emp => emp.AllowBirthdate).Where(emp => emp.BirthDate.HasValue).Where(emp => (emp.BirthDate.GetValueOrDefault().Day == date.Day) && (emp.BirthDate.GetValueOrDefault().Month == date.Month))
                           , fcSt => fcSt.idEmployee.Value, empl => empl.id,
                           (fcSt, empl) => new Employee
                           {
                               idEmployee = empl.id,
                               EmployeeFullName = empl.LastName + " " + empl.FirstName + " " + empl.Otch,
                               //idPost = 0,
                               //PostName = null,
                               //StaffCount = 0,
                               //idFactStaff = fcSt.idFactStaff.Value,
                               EmployeeUserName = Convert.ToString(empl.EmployeeLogin),
                               YearsOld = date.Year - empl.BirthDate.GetValueOrDefault().Year
                               //DepartmentName = fcSt.DepartmentName
                               
                           }).Distinct().ToArray(); 
               }
               else
                   employees =
                       ctx.GetDepartmentStaff(idDepartment, date, date, true).Join(ctx.Employees.Where(emp => emp.AllowBirthdate).Where(emp => emp.BirthDate.HasValue).Where(emp => (emp.BirthDate.GetValueOrDefault().Day == date.Day) && (emp.BirthDate.GetValueOrDefault().Month == date.Month))
                           , fcSt => fcSt.idEmployee.Value, empl => empl.id,
                           (fcSt, empl) => new Employee
                                               {
                                                   idEmployee = empl.id,
                                                   EmployeeFullName = empl.LastName + " " + empl.FirstName + " " + empl.Otch,
                                                   idPost = fcSt.idPost.Value,
                                                   PostName = fcSt.PostName,
                                                   StaffCount = fcSt.factStaffCount.Value,
                                                   ManagerBit = fcSt.ManagerBit.Value,
                                                   idFactStaff = fcSt.idFactStaff.Value,
                                                   EmployeeUserName = Convert.ToString(empl.EmployeeLogin),
                                                   YearsOld = date.Year - empl.BirthDate.GetValueOrDefault().Year,
                                                   DepartmentName = fcSt.DepartmentName
                                               }).ToArray(); 
                return employees;
           }


       }

       [WebMethod(Description = "Возвращает является ли сотрудник ППС.", CacheDuration = 21600)]
       public bool IsPPSByUserName(string userName)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               var employee = ctx.Employees.Where(empl => empl.EmployeeLogin == userName).FirstOrDefault();
               if (employee != null)
               {
                   var PPSStaffs = ctx.GetFactStaffByPeriod(DateTime.Today.Date, DateTime.Today.Date).Where(fcSt => fcSt.idEmployee.Value == employee.id).ToArray().Join(ctx.PlanStaffs,
                        fcSt => fcSt.idPlanStaff.Value, plSt => plSt.id, (fcSt, plSt) =>
                            new { idPost = plSt.idPost }).ToArray().Join(ctx.Posts.Where(p => p.Category.IsPPS.Value), fcSt => fcSt.idPost, post => post.id,
                            (fcSt, post) => new { idPost = post.id });
                   //если сотрудник не указан, завершение
                   if (PPSStaffs.Count()>0)
                       return true;
               }
               return false;
           }
           
       }

       [WebMethod(Description = "Возвращает список приемов/увольнений сотрудников за переданный период.", CacheDuration = 21600)]
       public FactStaffChange[] GetFactStaffChanges(DateTime PeriodBegin, DateTime PeriodEnd)
       {
           using (DBKadrDataContext ctx = new DBKadrDataContext("Data Source=ugtudb.ugtu.net;Initial Catalog=Kadr;Integrated Security=True"))
           {
               FactStaffChange[] factStaffChanges = ctx.GetFactStaffChangesByPeriod(1,PeriodBegin, PeriodEnd,true).ToArray().Select(fcSt
                       => new FactStaffChange
                       {
                           EmployeeName = fcSt.EmployeeName,
                           PostName = fcSt.PostName,
                           DepartmentName = fcSt.DepartmentName,
                           OperationDate = fcSt.OperationDate,
                           OperationName = fcSt.OperationName,
                           EmployeeUserName = fcSt.EmployeeLogin
                       }).ToArray();
               return factStaffChanges;
           }
       }
 
    }
}