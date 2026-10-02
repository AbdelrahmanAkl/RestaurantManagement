/*
NEW ROLES
1 = SuperAdmin
2 = Admin
3 = RestaurantManager
4 = BranchManager
5 = Waiter
6 = Kitchen
7 = Cashier

OLD ROLES
1 = Admin
2 = Manager
3 = Waiter
4 = Kitchen

OLD -> NEW
old Admin   -> SuperAdmin
old Manager -> RestaurantManager
old Waiter  -> Waiter
old Kitchen -> Kitchen
*/

BEGIN TRANSACTION;

UPDATE Users
SET Role =
    CASE Role
        WHEN 1 THEN 1
        WHEN 2 THEN 3
        WHEN 3 THEN 5
        WHEN 4 THEN 6
        ELSE Role
    END
WHERE Role IN (1, 2, 3, 4);

COMMIT TRANSACTION;

/*
Restaurant owner:
UPDATE Users
SET Role = 2, RestaurantId = 1, BranchId = NULL
WHERE Id = 10;

Restaurant manager:
UPDATE Users
SET Role = 3, RestaurantId = 1, BranchId = NULL
WHERE Id = 11;

Branch manager:
UPDATE Users
SET Role = 4, RestaurantId = 1, BranchId = 2
WHERE Id = 12;

Waiter:
UPDATE Users
SET Role = 5, RestaurantId = 1, BranchId = 2
WHERE Id = 13;

Kitchen:
UPDATE Users
SET Role = 6, RestaurantId = 1, BranchId = 2
WHERE Id = 14;

Cashier:
UPDATE Users
SET Role = 7, RestaurantId = 1, BranchId = 2
WHERE Id = 15;
*/
