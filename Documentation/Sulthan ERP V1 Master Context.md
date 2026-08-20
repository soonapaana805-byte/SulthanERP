Project Name : Sulthan ERP V1
Version      : V1
Started      : July 2026
Last Updated : 11-Aug-2026
Status       : 95% Complete

Sulthan ERP V1 – Master Context
1. Project Overview

Project Name: Sulthan ERP V1

Type:
Restaurant POS & Restaurant Management System

Technology Stack

Backend
.NET 8 Web API
Entity Framework Core
SQL Server
Desktop
WPF (.NET 8)
Mobile
Flutter (Android)
Database
SQL Server
2. Applications
Cashier App (WPF)

Responsible for:

Billing
Payment
Kitchen Bills
Reports
Table Management
Settings
Dashboard
Discount Approval
Cancel/Void
Captain App (Flutter)

Responsible for:

Login
Table selection
Menu selection
Send KOT
Additional KOT
Bill Request
Table cleaning
Kitchen

NO Kitchen Display Screen

Kitchen uses:

Black & White Thermal Printer only
3. Restaurant Workflow
Available Table
        │
        ▼
Captain Selects Table
        │
        ▼
Create Order
        │
        ▼
Send KOT
        │
        ▼
Kitchen Thermal Printer
        │
        ▼
Additional KOTs (Optional)
        │
        ▼
Customer finishes food
        │
        ▼
Captain → Request Bill
        │
        ▼
Confirmation
(Print Bill / Cancel)
        │
        ▼
Customer Bill queued
        │
        ▼
Customer Bill printed
        │
        ▼
PaymentPending
        │
        ▼
Collect Payment
        │
        ▼
Paid Receipt
        │
        ▼
CleaningPending
        │
        ▼
Mark Clean
        │
        ▼
Available
4. Table Lifecycle
Available

↓

Occupied

↓

BillRequested

↓

PaymentPending

↓

CleaningPending

↓

Available
5. Order Types

Supported:

Dine In
Take Away
Home Delivery
Phone Order

Phone Order internally:

OrderType = Parcel

Remarks = "Phone order"
6. Printing
Kitchen

Automatically prints

KOT

and

KOT Cancellation Slip

No Kitchen Screen.

Customer

Automatically prints

Pending Bill

↓

Paid Receipt

Supports

58MM
80MM
7. Multiple KOT

One bill can contain

KOT-001

KOT-002

KOT-003

...

KOT-XXX
8. Selected KOT Cancellation

Supported

Only selected KOT cancelled.

Other KOTs remain Active.

Bill recalculated.

Discount reset.

Kitchen receives

*** KOT CANCELLED ***

slip.

9. Bill Cancellation

Supported

Only before payment.

Requires

Admin approval

Reason

Audit

10. Paid Bill Void

Supported

Only after payment.

Requires

Admin approval

Reason

Audit

Original payment preserved.

11. Discount Approval

Requires

Admin Username

Admin Password

Reason

Wrong credentials

↓

Discount NOT applied.

Correct credentials

↓

Discount applied.

Audit stored.

12. Printing Queue

Durable Queue

Supports

Pending

Retry

Recovery

File Mode

Physical Printer

No duplicate printing.

13. Receipt Formats

Supports

58MM

80MM

Current Production

80MM

Compact Bill Number

Example

20260811-009

↓

Bill: 009

Captain shown

Only

Dine In

Tendered

Only when required.

14. Completed Modules

Authentication

JWT

Users

Roles

Categories

Menu

Tables

Captain App

Kitchen Printing

Multiple KOT

Pending Orders

Phone Orders

Home Delivery

Billing

Split Payment

Discount Approval

Discount Audit

Cancel Bill

Void Bill

Selected KOT Cancellation

KOT Cancellation Audit

Customer Receipt

Customer Print Queue

Kitchen Print Queue

Dashboard (Basic)

Reports (Basic)

15. Remaining Work
High Priority
Kitchen Bills auto-refresh (if still pending)
Owner Dashboard enhancements
Backup & Restore
Installer / Publish
Final UAT
16. Coding Rules

Never delete historical records.

Everything uses Audit.

No hard delete.

Print jobs are durable.

No duplicate print jobs.

No duplicate audits.

All financial history immutable.

17. Production Rules

Kitchen

↓

Thermal Printer only

Customer Bill

↓

Automatic

Paid Receipt

↓

Automatic

Manual print

↓

Recovery only

18. Current Project Status

Estimated Completion

95%

Core restaurant operations are production-ready.

19. Conversation Rules (Important)

When continuing this project in a new chat:

Do not redesign completed modules.
Preserve existing architecture.
Prefer additive changes over breaking changes.
Never remove audit trails.
Never remove durable print queues.
Never change business rules without explicit approval.
Always analyze first, list affected files, and wait for approval before implementation.


=============================
CHANGE LOG
=============================

11-Aug-2026
-------------
✓ Receipt formatting completed
✓ Multiple KOT cancellation completed
✓ Auto bill printing completed
✓ Discount audit completed
✓ Void bill completed

Pending:
- Kitchen Bills auto refresh
- Owner Dashboard
- Backup & Restore
- Installer



******How to use this*******

Whenever you start a new chat or use another ChatGPT account, paste this Master Context first, then add a short update such as:

Latest status:

- Item 3 (Receipt Formatting): Completed.
- Captain "Request Bill" workflow: Auto print implemented.
- Remaining regression: Kitchen Bills auto-refresh after payment.
- Continue from here.

This gives the new conversation everything it needs to continue without losing project context.