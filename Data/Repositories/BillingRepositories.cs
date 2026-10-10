using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using HospitalSystem.Models;

namespace HospitalSystem.Data.Repositories
{
    public interface IBillRepository
    {
        // Bills with their items and payments.
        List<Bill> GetAll();

        // Inserts the bill, its items and its totals in one transaction.
        void Insert(Bill b);

        // Each write below also saves the bill's totals, in the same transaction.
        void InsertItem(Bill b, BillItem item);
        void DeleteItem(Bill b, int itemId);
        void UpdateItems(Bill b, IEnumerable<BillItem> items);
        void InsertPayment(Bill b, Payment p);
        void SaveTotals(Bill b);
    }

    public interface IChargeScheduleRepository
    {
        List<ChargeSchedule> GetAll();
        void Insert(ChargeSchedule c);
        void Update(ChargeSchedule c);
    }

    public class MySqlBillRepository : IBillRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlBillRepository(IDbConnectionFactory db) { this.db = db; }

        public List<Bill> GetAll()
        {
            var list = new List<Bill>();
            using (var conn = db.OpenConnection())
            {
                using (var cmd = new MySqlCommand(
                    "SELECT id, patient_id, admission_id, appointment_id, bill_date, total_amount, amount_paid, " +
                    "balance, status, notes, created_by, created_at, discount_value, discount_is_percent, discount_reason, " +
                    "eligibility, eligibility_id_no, vat_rate, hmo_provider, hmo_loa_no, hmo_coverage FROM bills", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        BillStatus status;
                        Enum.TryParse(r.GetString("status"), out status);

                        DiscountEligibility eligibility;
                        if (!Enum.TryParse(r.GetString("eligibility"), out eligibility))
                            eligibility = DiscountEligibility.None;

                        list.Add(new Bill
                        {
                            Id = r.GetInt32("id"),
                            PatientId = r.GetInt32("patient_id"),
                            AdmissionId = r.IsDBNull(r.GetOrdinal("admission_id")) ? (int?)null : r.GetInt32("admission_id"),
                            AppointmentId = r.IsDBNull(r.GetOrdinal("appointment_id")) ? (int?)null : r.GetInt32("appointment_id"),
                            BillDate = r.GetDateTime("bill_date"),
                            TotalAmount = r.GetDecimal("total_amount"),
                            AmountPaid = r.GetDecimal("amount_paid"),
                            Balance = r.GetDecimal("balance"),
                            Status = status,
                            Notes = Sql.ReadString(r, "notes"),
                            CreatedBy = Sql.ReadString(r, "created_by"),
                            CreatedAt = r.GetDateTime("created_at"),
                            Adjustments = new BillAdjustments
                            {
                                DiscountValue = r.GetDecimal("discount_value"),
                                DiscountIsPercent = r.GetBoolean("discount_is_percent"),
                                DiscountReason = Sql.ReadString(r, "discount_reason"),
                                Eligibility = eligibility,
                                EligibilityIdNo = Sql.ReadString(r, "eligibility_id_no"),
                                VatRate = r.GetDecimal("vat_rate"),
                                HmoProvider = Sql.ReadString(r, "hmo_provider"),
                                HmoLoaNo = Sql.ReadString(r, "hmo_loa_no"),
                                HmoCoverage = r.GetDecimal("hmo_coverage")
                            }
                        });
                    }
                }

                var byId = list.ToDictionary(b => b.Id);

                using (var cmd = new MySqlCommand(
                    "SELECT id, bill_id, description, category, quantity, unit_price, amount, per_day FROM bill_items", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Bill bill;
                        if (!byId.TryGetValue(r.GetInt32("bill_id"), out bill)) continue;

                        BillCategory category;
                        if (!Enum.TryParse(r.GetString("category"), out category))
                            category = BillCategory.Other;

                        bill.Items.Add(new BillItem
                        {
                            Id = r.GetInt32("id"),
                            BillId = bill.Id,
                            Description = r.GetString("description"),
                            Category = category,
                            Quantity = r.GetInt32("quantity"),
                            UnitPrice = r.GetDecimal("unit_price"),
                            Amount = r.GetDecimal("amount"),
                            PerDay = r.GetBoolean("per_day")
                        });
                    }
                }

                using (var cmd = new MySqlCommand(
                    "SELECT id, bill_id, amount, payment_method, payment_date, reference_no, received_by, " +
                    "amount_tendered, card_type, card_last4, approval_code, hmo_provider, hmo_loa_no FROM payments", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Bill bill;
                        if (!byId.TryGetValue(r.GetInt32("bill_id"), out bill)) continue;

                        PaymentMethod method;
                        Enum.TryParse(r.GetString("payment_method"), out method);

                        bill.Payments.Add(new Payment
                        {
                            Id = r.GetInt32("id"),
                            BillId = bill.Id,
                            Amount = r.GetDecimal("amount"),
                            Method = method,
                            PaymentDate = r.GetDateTime("payment_date"),
                            ReferenceNo = Sql.ReadString(r, "reference_no"),
                            ReceivedBy = Sql.ReadString(r, "received_by"),
                            AmountTendered = r.IsDBNull(r.GetOrdinal("amount_tendered")) ? (decimal?)null : r.GetDecimal("amount_tendered"),
                            CardType = Sql.ReadString(r, "card_type"),
                            CardLast4 = Sql.ReadString(r, "card_last4"),
                            ApprovalCode = Sql.ReadString(r, "approval_code"),
                            HmoProvider = Sql.ReadString(r, "hmo_provider"),
                            HmoLoaNo = Sql.ReadString(r, "hmo_loa_no")
                        });
                    }
                }
            }

            // Rebuild the breakdown (subtotal, discounts, VAT, HMO) from the items and adjustments.
            foreach (var bill in list)
            {
                bill.CalculateTotal();
                bill.CalculateBalance();
            }

            return list;
        }

        public void Insert(Bill b)
        {
            using (var conn = db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO bills (patient_id, admission_id, appointment_id, bill_date, total_amount, amount_paid, " +
                    "balance, status, notes, created_by, created_at) " +
                    "VALUES (@patientId, @admissionId, @appointmentId, @billDate, @total, @paid, @balance, @status, " +
                    "@notes, @createdBy, @createdAt); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@patientId", b.PatientId);
                    cmd.Parameters.AddWithValue("@admissionId", Sql.OrNull(b.AdmissionId));
                    cmd.Parameters.AddWithValue("@appointmentId", Sql.OrNull(b.AppointmentId));
                    cmd.Parameters.AddWithValue("@billDate", b.BillDate);
                    cmd.Parameters.AddWithValue("@total", b.TotalAmount);
                    cmd.Parameters.AddWithValue("@paid", b.AmountPaid);
                    cmd.Parameters.AddWithValue("@balance", b.Balance);
                    cmd.Parameters.AddWithValue("@status", b.Status.ToString());
                    cmd.Parameters.AddWithValue("@notes", Sql.OrNull(b.Notes));
                    cmd.Parameters.AddWithValue("@createdBy", Sql.OrNull(b.CreatedBy));
                    cmd.Parameters.AddWithValue("@createdAt", b.CreatedAt);
                    b.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var item in b.Items)
                {
                    item.BillId = b.Id;
                    InsertItem(item, conn, tx);
                }

                SaveTotals(b, conn, tx);   // breakdown and adjustment columns
                tx.Commit();
            }
        }

        public void InsertItem(Bill b, BillItem item)
        {
            InTransaction(b, (conn, tx) => InsertItem(item, conn, tx));
        }

        public void DeleteItem(Bill b, int itemId)
        {
            InTransaction(b, (conn, tx) =>
            {
                using (var cmd = new MySqlCommand("DELETE FROM bill_items WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void UpdateItems(Bill b, IEnumerable<BillItem> items)
        {
            InTransaction(b, (conn, tx) =>
            {
                foreach (var item in items)
                {
                    using (var cmd = new MySqlCommand(
                        "UPDATE bill_items SET quantity=@q, amount=@a WHERE id=@id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@q", item.Quantity);
                        cmd.Parameters.AddWithValue("@a", item.Amount);
                        cmd.Parameters.AddWithValue("@id", item.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        public void InsertPayment(Bill b, Payment p)
        {
            InTransaction(b, (conn, tx) =>
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO payments (bill_id, amount, payment_method, payment_date, reference_no, received_by, " +
                    "amount_tendered, card_type, card_last4, approval_code, hmo_provider, hmo_loa_no) " +
                    "VALUES (@billId, @amount, @method, @date, @ref, @receivedBy, " +
                    "@tendered, @cardType, @cardLast4, @approval, @hmoProvider, @hmoLoa); SELECT LAST_INSERT_ID();", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@billId", p.BillId);
                    cmd.Parameters.AddWithValue("@amount", p.Amount);
                    cmd.Parameters.AddWithValue("@method", p.Method.ToString());
                    cmd.Parameters.AddWithValue("@date", p.PaymentDate);
                    cmd.Parameters.AddWithValue("@ref", Sql.NullIfBlank(p.ReferenceNo));
                    cmd.Parameters.AddWithValue("@receivedBy", Sql.NullIfBlank(p.ReceivedBy));
                    cmd.Parameters.AddWithValue("@tendered", Sql.OrNull(p.AmountTendered));
                    cmd.Parameters.AddWithValue("@cardType", Sql.NullIfBlank(p.CardType));
                    cmd.Parameters.AddWithValue("@cardLast4", Sql.NullIfBlank(p.CardLast4));
                    cmd.Parameters.AddWithValue("@approval", Sql.NullIfBlank(p.ApprovalCode));
                    cmd.Parameters.AddWithValue("@hmoProvider", Sql.NullIfBlank(p.HmoProvider));
                    cmd.Parameters.AddWithValue("@hmoLoa", Sql.NullIfBlank(p.HmoLoaNo));
                    p.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
            });
        }

        public void SaveTotals(Bill b)
        {
            InTransaction(b, (conn, tx) => { });
        }

        // Runs the write, then saves the bill's totals, and commits both together.
        private void InTransaction(Bill b, Action<MySqlConnection, MySqlTransaction> write)
        {
            using (var conn = db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                write(conn, tx);
                SaveTotals(b, conn, tx);
                tx.Commit();
            }
        }

        private static void InsertItem(BillItem item, MySqlConnection conn, MySqlTransaction tx)
        {
            using (var cmd = new MySqlCommand(
                "INSERT INTO bill_items (bill_id, description, category, quantity, unit_price, amount, per_day) " +
                "VALUES (@billId, @description, @category, @quantity, @unitPrice, @amount, @perDay); SELECT LAST_INSERT_ID();", conn, tx))
            {
                cmd.Parameters.AddWithValue("@billId", item.BillId);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@category", item.Category.ToString());
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                cmd.Parameters.AddWithValue("@amount", item.Amount);
                cmd.Parameters.AddWithValue("@perDay", item.PerDay);
                item.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Saves the totals, the full breakdown and the adjustment inputs.
        private static void SaveTotals(Bill b, MySqlConnection conn, MySqlTransaction tx)
        {
            var adj = b.Adjustments;
            using (var cmd = new MySqlCommand(
                "UPDATE bills SET total_amount=@total, amount_paid=@paid, balance=@balance, status=@status, " +
                "subtotal=@subtotal, discount_value=@discountValue, discount_is_percent=@discountIsPercent, " +
                "discount_reason=@discountReason, discount_amount=@discountAmount, eligibility=@eligibility, " +
                "eligibility_id_no=@eligibilityIdNo, statutory_discount=@statutoryDiscount, vat_rate=@vatRate, " +
                "vat_amount=@vatAmount, hmo_provider=@hmoProvider, hmo_loa_no=@hmoLoaNo, hmo_coverage=@hmoCoverage, " +
                "hmo_amount=@hmoAmount WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@total", b.TotalAmount);
                cmd.Parameters.AddWithValue("@paid", b.AmountPaid);
                cmd.Parameters.AddWithValue("@balance", b.Balance);
                cmd.Parameters.AddWithValue("@status", b.Status.ToString());
                cmd.Parameters.AddWithValue("@subtotal", b.Subtotal);
                cmd.Parameters.AddWithValue("@discountValue", adj.DiscountValue);
                cmd.Parameters.AddWithValue("@discountIsPercent", adj.DiscountIsPercent);
                cmd.Parameters.AddWithValue("@discountReason", Sql.NullIfBlank(adj.DiscountReason));
                cmd.Parameters.AddWithValue("@discountAmount", b.DiscountAmount);
                cmd.Parameters.AddWithValue("@eligibility", adj.Eligibility.ToString());
                cmd.Parameters.AddWithValue("@eligibilityIdNo", Sql.NullIfBlank(adj.EligibilityIdNo));
                cmd.Parameters.AddWithValue("@statutoryDiscount", b.StatutoryDiscountAmount);
                cmd.Parameters.AddWithValue("@vatRate", adj.VatRate);
                cmd.Parameters.AddWithValue("@vatAmount", b.VatAmount);
                cmd.Parameters.AddWithValue("@hmoProvider", Sql.NullIfBlank(adj.HmoProvider));
                cmd.Parameters.AddWithValue("@hmoLoaNo", Sql.NullIfBlank(adj.HmoLoaNo));
                cmd.Parameters.AddWithValue("@hmoCoverage", adj.HmoCoverage);
                cmd.Parameters.AddWithValue("@hmoAmount", b.HmoAmount);
                cmd.Parameters.AddWithValue("@id", b.Id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class MySqlChargeScheduleRepository : IChargeScheduleRepository
    {
        private readonly IDbConnectionFactory db;
        public MySqlChargeScheduleRepository(IDbConnectionFactory db) { this.db = db; }

        public List<ChargeSchedule> GetAll()
        {
            var list = new List<ChargeSchedule>();
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "SELECT id, description, category, unit_price, ward, per_day, is_active FROM charge_schedules", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    BillCategory category;
                    if (!Enum.TryParse(r.GetString("category"), out category))
                        category = BillCategory.Other;

                    list.Add(new ChargeSchedule
                    {
                        Id = r.GetInt32("id"),
                        Description = r.GetString("description"),
                        Category = category,
                        UnitPrice = r.GetDecimal("unit_price"),
                        Ward = Sql.ReadString(r, "ward"),
                        PerDay = r.GetBoolean("per_day"),
                        IsActive = r.GetBoolean("is_active")
                    });
                }
            }
            return list;
        }

        public void Insert(ChargeSchedule c)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "INSERT INTO charge_schedules (description, category, unit_price, ward, per_day, is_active) " +
                "VALUES (@description, @category, @unitPrice, @ward, @perDay, @isActive); SELECT LAST_INSERT_ID();", conn))
            {
                AddParameters(cmd, c);
                c.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(ChargeSchedule c)
        {
            using (var conn = db.OpenConnection())
            using (var cmd = new MySqlCommand(
                "UPDATE charge_schedules SET description=@description, category=@category, unit_price=@unitPrice, " +
                "ward=@ward, per_day=@perDay, is_active=@isActive WHERE id=@id", conn))
            {
                AddParameters(cmd, c);
                cmd.Parameters.AddWithValue("@id", c.Id);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddParameters(MySqlCommand cmd, ChargeSchedule c)
        {
            cmd.Parameters.AddWithValue("@description", c.Description);
            cmd.Parameters.AddWithValue("@category", c.Category.ToString());
            cmd.Parameters.AddWithValue("@unitPrice", c.UnitPrice);
            cmd.Parameters.AddWithValue("@ward", string.IsNullOrEmpty(c.Ward) ? (object)DBNull.Value : c.Ward);
            cmd.Parameters.AddWithValue("@perDay", c.PerDay);
            cmd.Parameters.AddWithValue("@isActive", c.IsActive);
        }
    }
}
