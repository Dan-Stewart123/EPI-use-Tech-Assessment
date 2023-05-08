using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EPI_use_Tech_Assessment.Models;
using jsTree3.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace EPI_use_Tech_Assessment.Controllers
{
    public class EmployeesController : Controller
    {
        private EmployeeDBEntities db = new EmployeeDBEntities();

        public static string encodePassword(string password)
        {
            try
            {
                int len = password.Length;
                byte[] encPass_byte = new byte[len];
                encPass_byte = System.Text.Encoding.UTF8.GetBytes(password);
                string encodedPassword = Convert.ToBase64String(encPass_byte);
                Console.Write(encodedPassword);
                return encodedPassword;
            }// try
            catch (Exception ex)
            {
                throw new Exception("Error in base64Encode" + ex);
            }// catch
        }// method to encode the password

        public string decodePassword(string encodedPassword)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();
            byte[] todecode_byte = Convert.FromBase64String(encodedPassword);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string decodedPassword = new String(decoded_char);
            return decodedPassword;
        }// method to decode the password

        public ActionResult loginPage()
        {
            try
            {
                if (Session["AuthID"] != null)
                {
                    Response.Cookies["AuthID"].Expires = DateTime.Now.AddDays(-30);
                    Session.Clear();
                }
            }
            catch
            {

            }
            return View();
        }// returns Login screen

        [HttpPost]
        public ActionResult loginPage(string email, string password)
        {
            string passwordFromDb = "";
            string authId = Guid.NewGuid().ToString();

            Session["AuthID"] = authId;

            var cookie = new HttpCookie("AuthID");
            cookie.Value = authId;
            Response.Cookies.Add(cookie);
            Employee loginUser = new Employee();
            if (email != "" || password != "")
            {
                foreach (Employee tempUser in db.Employees.ToList())
                {
                    if (email == tempUser.email)
                    {
                        loginUser = tempUser;
                        foreach (Password tempPass in db.Passwords.ToList())
                        {
                            if (loginUser.EmployeeID == tempPass.EmployeeID)
                            {
                                passwordFromDb = decodePassword(tempPass.EncryptedPassword);
                                if (password == passwordFromDb)
                                {
                                    return RedirectToAction("homePage", new { id = loginUser.EmployeeID });
                                }// checks is entered password matches db password
                            }// matches user and password ids
                        }// searches passwords
                    }// matches username to db username
                }// searches users
                if (email == "")
                {
                    ViewData["err"] = "Please enter your email address";
                }// if username null
                else
                {
                    ViewData["err"] = "Username or password is incorrect";
                }// else
                return View();
            }
            else
            {
                if (password == "")
                {
                    ViewData["err"] = "Please enter your password";
                }// password null
                else
                {
                    ViewData["err"] = "Please complete all the required fields";
                }//else
                return View();
            }
            // verify username and password here, if correct then display home screen, else login screen with a pop up
        }// returns home screen

        public ActionResult createAccount()
        {
            return View(db.Employees.ToList());
        }// return create account screen

        [HttpPost]
        public ActionResult createAccount(string firstName, string lastName, string dob, string email, int? empNum, int? salary, string pos, int? rlm, string password, string confPass)
        {
            if (firstName == "" || lastName == "" || dob == "" || email == "" || empNum == null || salary == null || pos == "" || rlm == null || password == "" || confPass == "")
            {
                ViewData["err"] = "Please complete all the required fields";
                return View(db.Employees.ToList());
            }// if all fields are empty
            if (password != confPass)
            {
                ViewData["err"] = "Passwords do not match";
                return View(db.Employees.ToList());
            }// checks that passwords match
            if (password.Length < 8 || confPass.Length < 8)
            {
                ViewData["err"] = "Password is not long enough.";
                return View(db.Employees.ToList());
            }// checks password length
            if (email.Contains("@") == false)
            {
                ViewData["err"] = "Please enter a valid email address.";
                return View(db.Employees.ToList());
            }// validated email
            else
            {
                Employee newEmp = new Employee();
                newEmp.FName = firstName;
                newEmp.LName = lastName;
                newEmp.DOB = Convert.ToDateTime(dob);
                newEmp.EmpNumber = empNum;
                newEmp.Salary = salary;
                newEmp.Position = pos;
                newEmp.Manager = rlm;
                newEmp.email = email;

                db.Employees.Add(newEmp);
                db.SaveChanges();

                Password newPass = new Password();
                newPass.EmployeeID = db.Employees.ToList().Find(e => e.EmpNumber == empNum).EmployeeID;
                string encodedpass = encodePassword(confPass);
                newPass.EncryptedPassword = encodedpass;

                db.Passwords.Add(newPass);
                db.SaveChanges();
            }// else

            return RedirectToAction("loginPage");
        }// create account post method

        public void SetPageCacheNoStore()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.AppendCacheExtension("no-store, must-revalidate");
            Response.AppendHeader("Pragma", "no-cache");
            Response.AppendHeader("Expires", "0");
        }

        public ActionResult homePage(int? id)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    Employee loggedEmployee = db.Employees.ToList().Find(u => u.EmployeeID == id);

                    IList<JsTree3Node> empList = new List<JsTree3Node>();

                    foreach (var emp in db.Employees.ToList())
                    {
                        JsTree3Node temp = new JsTree3Node();
                        temp.id = "" + emp.EmployeeID;
                        temp.text = emp.FName + " " + emp.LName;
                        if (emp.Manager == 0)
                        {
                            temp.parent = "#";
                            temp.state = new State(true, false, false);
                        }// if statement
                        else
                        {
                            temp.parent = "" + emp.Manager;
                            temp.state = new State(true, false, false);
                        }// else 
                        temp.icon = false.ToString();

                        empList.Add(temp);
                    }// for each

                    ViewBag.json = JsonSerializer.Serialize(empList);

                    MD5 md5 = MD5.Create();

                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(loggedEmployee.email);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    var sb = new StringBuilder();
                    foreach (var t in hashBytes) sb.Append(t.ToString("X2"));

                    var temp2 = sb.ToString().ToLower();

                    ViewData["hash"] = sb.ToString().ToLower();// hashes employees email to create gravatar link in view

                    SetPageCacheNoStore();
                    return View(loggedEmployee);
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// returns home page


        public ActionResult deletePage(int? id)
        {

            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    MD5 md5 = MD5.Create();

                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(db.Employees.ToList().Find(e => e.EmployeeID == id).email);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    var sb = new StringBuilder();
                    foreach (var t in hashBytes) sb.Append(t.ToString("X2"));

                    ViewData["hash"] = sb.ToString().ToLower();

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }

        }// delete account get


        public ActionResult deleteConformation(int? id)
        {

            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    Employee delEmployee = db.Employees.ToList().Find(e => e.EmployeeID == id);
                    Password delPass = db.Passwords.ToList().Find(p => p.EmployeeID == id);

                    db.Passwords.Remove(delPass);
                    db.SaveChanges();

                    db.Employees.Remove(delEmployee);
                    db.SaveChanges();

                    return RedirectToAction("loginPage");
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }

        }// delete account conformation

        public ActionResult updateAccount(int? id)
        {

            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    List<SelectListItem> selListEmployees = new List<SelectListItem>();
                    SelectListItem noManager = new SelectListItem();
                    noManager.Value = "0";
                    noManager.Text = "No Manager";
                    selListEmployees.Add(noManager);

                    foreach (var emp in db.Employees.ToList())
                    {
                        SelectListItem temp = new SelectListItem();
                        temp.Value = "" + emp.EmployeeID;
                        temp.Text = emp.FName + " " + emp.LName;
                        selListEmployees.Add(temp);
                    }// populating select list 
                    ViewBag.data = selListEmployees;

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }

        }// update account get

        [HttpPost]
        public ActionResult updateAccount(int? id, string firstName, string lastName, string dob, string email, int? empNum, int? salary, string pos, string rlm)
        {

            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    if (firstName == "" || lastName == "" || dob == "" || email == "" || empNum == null || salary == null || pos == "" || rlm == "")
                    {
                        List<SelectListItem> selListEmployees = new List<SelectListItem>();
                        SelectListItem noManager = new SelectListItem();
                        noManager.Value = "0";
                        noManager.Text = "No Manager";
                        selListEmployees.Add(noManager);

                        foreach (var emp in db.Employees.ToList())
                        {
                            SelectListItem temp = new SelectListItem();
                            temp.Value = "" + emp.EmployeeID;
                            temp.Text = emp.FName + " " + emp.LName;
                            selListEmployees.Add(temp);
                        }// populating select list 
                        ViewBag.data = selListEmployees;
                        ViewData["err"] = "Please complete all the required fields";
                        return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                    }// if all fields are empty
                    if (email.Contains("@") == false)
                    {
                        List<SelectListItem> selListEmployees = new List<SelectListItem>();
                        SelectListItem noManager = new SelectListItem();
                        noManager.Value = "0";
                        noManager.Text = "No Manager";
                        selListEmployees.Add(noManager);

                        foreach (var emp in db.Employees.ToList())
                        {
                            SelectListItem temp = new SelectListItem();
                            temp.Value = "" + emp.EmployeeID;
                            temp.Text = emp.FName + " " + emp.LName;
                            selListEmployees.Add(temp);
                        }// populating select list 
                        ViewBag.data = selListEmployees;
                        ViewData["err"] = "Please enter a valid email address.";
                        return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                    }// validated email
                    else
                    {
                        Employee oldEmp = db.Employees.ToList().Find(e => e.EmployeeID == id);
                        Employee newEmp = oldEmp;
                        newEmp.FName = firstName;
                        newEmp.LName = lastName;
                        newEmp.DOB = Convert.ToDateTime(dob);
                        newEmp.email = email;
                        newEmp.EmpNumber = empNum;
                        newEmp.Salary = Convert.ToInt32(salary);
                        newEmp.Position = pos;
                        newEmp.Manager = Convert.ToInt32(rlm);

                        db.Entry(newEmp).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }// else statement

                    return RedirectToAction("homePage", new { id = id });
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }

        }// update account post

        public ActionResult changePassword(int? id)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// change password get

        [HttpPost]
        public ActionResult changePassword(int? id, string oldPass, string newPass, string confPass)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    if (oldPass == "" || newPass == "" || confPass == "")
                    {
                        ViewData["err"] = "Please complete all the required fields";
                        return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                    }// if fields are empty
                    if (newPass != confPass)
                    {
                        ViewData["err"] = "Passwords don't match.";
                        return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                    }// if passwords dont match
                    if (newPass.Length < 8 || confPass.Length < 8)
                    {
                        ViewData["err"] = "Password is not long enough.";
                        return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                    }// checks password length
                    else
                    {
                        Employee tempEmp = db.Employees.ToList().Find(e => e.EmployeeID == id);
                        Password pass = db.Passwords.ToList().Find(p => p.EmployeeID == tempEmp.EmployeeID);

                        string decPass = decodePassword(pass.EncryptedPassword);
                        if (decPass == oldPass)
                        {
                            string encNewPass = encodePassword(confPass);
                            pass.EncryptedPassword = encNewPass;

                            db.Entry(pass).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();
                            return RedirectToAction("loginPage");
                        }// if passwords match
                        else
                        {
                            string temp = "Old password is incorrect.";
                            return RedirectToAction("resetPassword", new { id = id, err = temp });
                        }// if passwords dont match
                    }// else statement
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// change passwords post

        public ActionResult forgotPassword()
        {
            return View();
        }// forgot password get

        [HttpPost]
        public ActionResult forgotPassword(string email, string newPass, string confPass)
        {
            if (email == "" || newPass == "" || confPass == "")
            {
                ViewData["err"] = "Please complete all the required fields";
                return View();
            }// if fields are empty
            if (newPass != confPass)
            {
                return View();
            }// if passwords dont match
            if (newPass.Length < 8 || confPass.Length < 8)
            {
                ViewData["err"] = "Password is not long enough.";
                return View();
            }// checks password length
            else
            {
                Employee temp = db.Employees.ToList().Find(e => e.email == email);
                if (temp == null)
                {
                    ViewData["err"] = "Email address not found.";
                    return View();
                }// if email does not exist
                else
                {
                    Password pass = db.Passwords.ToList().Find(p => p.EmployeeID == temp.EmployeeID);
                    pass.EncryptedPassword = encodePassword(confPass);
                    db.Entry(pass).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("loginPage");
                }
            }
        }// forgot password post

        public ActionResult sortEmployees(int? id)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// sort employees get method

        [HttpPost]
        public ActionResult sortEmployees(int? id, string sort)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    List<Employee> sortedList = new List<Employee>();
                    if (sort == "1")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.FName).ToList();
                    }// first name
                    if (sort == "2")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.LName).ToList();
                    }// surname
                    if (sort == "3")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.DOB).ToList();
                    }// dob
                    if (sort == "4")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.EmpNumber).ToList();
                    }// employee number
                    if (sort == "5")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.Salary).ToList();
                    }// salary asc
                    if (sort == "6")
                    {
                        sortedList = db.Employees.ToList().OrderByDescending(e => e.LName).ToList();
                    }// salary dec
                    if (sort == "7")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.Position).ToList();
                    }// position
                    if (sort == "8")
                    {
                        sortedList = db.Employees.ToList().OrderBy(e => e.Manager).ToList();
                    }// manager

                    ViewData["employees"] = sortedList;

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// sort employees post

        public ActionResult searchEmployees(int? id)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// search employees get method

        [HttpPost]
        public ActionResult searchEmployees(int? id, string sort, string search)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    List<Employee> sortedList = new List<Employee>();
                    if (search == "")
                    {
                        ViewData["err"] = "Please enter a search parameter.";
                        return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                    }
                    if (sort == "1")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.FName == search);
                    }// first name
                    if (sort == "2")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.LName == search);
                    }// surname
                    if (sort == "3")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.DOB == Convert.ToDateTime(search));
                    }// dob
                    if (sort == "4")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.EmpNumber == Convert.ToInt32(search));
                    }// employee number
                    if (sort == "5")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.Salary == Convert.ToInt32(search));
                    }// salary 
                    if (sort == "6")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.email == search);
                    }// email
                    if (sort == "7")
                    {
                        sortedList = db.Employees.ToList().FindAll(e => e.Position == search);
                    }// position
                    if (sort == "8")
                    {
                        Employee temp = db.Employees.ToList().Find(emp => emp.LName == search);
                        sortedList = db.Employees.ToList().FindAll(e => e.Manager == temp.EmployeeID);
                    }// manager

                    ViewData["employees"] = sortedList;

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == id));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// sort employees post

        public ActionResult viewEmployeePage(int? id, int? empID)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    ViewData["id"] = id;
                    Employee temp = db.Employees.ToList().Find(e => e.EmployeeID == empID);
                    Employee temp2 = db.Employees.ToList().Find(e => e.EmployeeID == temp.Manager);
                    if (temp2 != null)
                    {
                        ViewData["manager"] = temp2.FName + " " + temp2.LName;
                    }// if employee has a manager
                    else
                    {
                        ViewData["manager"] = "None";
                    }// if the employee does not have a manager

                    MD5 md5 = MD5.Create();

                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(db.Employees.ToList().Find(e => e.EmployeeID == empID).email);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    var sb = new StringBuilder();
                    foreach (var t in hashBytes) sb.Append(t.ToString("X2"));

                    ViewData["hash"] = sb.ToString().ToLower();// hashes employees email to creata gravatar link in view

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == empID));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// view employee page

        public ActionResult deleteOtherEmployee(int? id, int? empID)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    ViewData["id"] = id;
                    Employee temp = db.Employees.ToList().Find(e => e.EmployeeID == empID);
                    Employee temp2 = db.Employees.ToList().Find(e => e.EmployeeID == temp.Manager);
                    if (temp2 != null)
                    {
                        ViewData["manager"] = temp2.FName + " " + temp2.LName;
                    }// if employee has a manager
                    else
                    {
                        ViewData["manager"] = "None";
                    }// if the employee does not have a manager

                    MD5 md5 = MD5.Create();

                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(db.Employees.ToList().Find(e => e.EmployeeID == empID).email);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    var sb = new StringBuilder();
                    foreach (var t in hashBytes) sb.Append(t.ToString("X2"));

                    ViewData["hash"] = sb.ToString().ToLower();// hashes employees email to create gravatar link in the view. 

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == empID));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// delete reporting employee get

        public ActionResult deleteOtherConformation(int? id, int? delID)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    Employee delEmployee = db.Employees.ToList().Find(e => e.EmployeeID == delID);
                    Password delPass = db.Passwords.ToList().Find(p => p.EmployeeID == delID);

                    db.Passwords.Remove(delPass);
                    db.SaveChanges();

                    db.Employees.Remove(delEmployee);
                    db.SaveChanges();

                    return RedirectToAction("searchEmployees", new { id = id });
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// delete reporting employee conformation

        public ActionResult updateOtherEmployee(int? id, int? empID)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    ViewData["id"] = id;
                    List<SelectListItem> selListEmployees = new List<SelectListItem>();
                    SelectListItem noManager = new SelectListItem();
                    noManager.Value = "0";
                    noManager.Text = "No Manager";
                    selListEmployees.Add(noManager);

                    foreach (var emp in db.Employees.ToList())
                    {
                        SelectListItem temp = new SelectListItem();
                        temp.Value = "" + emp.EmployeeID;
                        temp.Text = emp.FName + " " + emp.LName;
                        selListEmployees.Add(temp);
                    }// populating select list 
                    ViewBag.data = selListEmployees;

                    return View(db.Employees.ToList().Find(e => e.EmployeeID == empID));
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// update reporting employee get

        [HttpPost]
        public ActionResult updateOtherEmployee(int? id, int? empID, string firstName, string lastName, string dob, string email, int? empNum, int? salary, string pos, string rlm)
        {
            try
            {
                if (Request.Cookies["AuthID"].Value == Session["AuthID"].ToString())
                {
                    if (firstName == "" || lastName == "" || dob == "" || email == "" || empNum == null || salary == null || pos == "" || rlm == "")
                    {
                        ViewData["err"] = "Please complete all the required fields";
                        ViewData["id"] = id;
                        List<SelectListItem> selListEmployees = new List<SelectListItem>();
                        SelectListItem noManager = new SelectListItem();
                        noManager.Value = "0";
                        noManager.Text = "No Manager";
                        selListEmployees.Add(noManager);

                        foreach (var emp in db.Employees.ToList())
                        {
                            SelectListItem temp = new SelectListItem();
                            temp.Value = "" + emp.EmployeeID;
                            temp.Text = emp.FName + " " + emp.LName;
                            selListEmployees.Add(temp);
                        }// populating select list 
                        ViewBag.data = selListEmployees;

                        return View(db.Employees.ToList().Find(e => e.EmployeeID == empID));
                    }// if all fields are empty
                    if (email.Contains("@") == false)
                    {
                        ViewData["err"] = "Please enter a valid email address.";
                        ViewData["id"] = id;
                        List<SelectListItem> selListEmployees = new List<SelectListItem>();
                        SelectListItem noManager = new SelectListItem();
                        noManager.Value = "0";
                        noManager.Text = "No Manager";
                        selListEmployees.Add(noManager);

                        foreach (var emp in db.Employees.ToList())
                        {
                            SelectListItem temp = new SelectListItem();
                            temp.Value = "" + emp.EmployeeID;
                            temp.Text = emp.FName + " " + emp.LName;
                            selListEmployees.Add(temp);
                        }// populating select list 
                        ViewBag.data = selListEmployees;

                        return View(db.Employees.ToList().Find(e => e.EmployeeID == empID));
                    }// validated email
                    else
                    {
                        Employee oldEmp = db.Employees.ToList().Find(e => e.EmployeeID == empID);
                        Employee newEmp = oldEmp;
                        newEmp.FName = firstName;
                        newEmp.LName = lastName;
                        newEmp.DOB = Convert.ToDateTime(dob);
                        newEmp.email = email;
                        newEmp.EmpNumber = empNum;
                        newEmp.Salary = Convert.ToInt32(salary);
                        newEmp.Position = pos;
                        newEmp.Manager = Convert.ToInt32(rlm);

                        db.Entry(newEmp).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }// else statement

                    return RedirectToAction("searchEmployees", new { id = id });
                }
                else
                {
                    return RedirectToAction("loginPage");
                }
            }
            catch
            {
                return RedirectToAction("loginPage");
            }
        }// update reporting employee post














        // GET: Employees
        public ActionResult Index()
        {
            return View(db.Employees.ToList());
        }

        // GET: Employees/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // GET: Employees/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EmployeeID,FName,LName,DOB,EmpNumber,Salary,Position,Manager,email")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                db.Employees.Add(employee);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(employee);
        }

        // GET: Employees/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EmployeeID,FName,LName,DOB,EmpNumber,Salary,Position,Manager,email")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                db.Entry(employee).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Employee employee = db.Employees.Find(id);
            db.Employees.Remove(employee);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
