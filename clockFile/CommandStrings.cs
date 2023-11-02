using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clockFile
{
    public static class CommandStrings
    {
        public static string selectPerno = $@"select perno 
                                        from pcdcprel 
                                        where pcdtype = '2' and cardno = @cardno and start_date <= @startDate
                                        order by start_date desc";


        public static string selectPaymark1Paymark4Paymark5Deptno = $@"select paymark1, paymark4, paymark5, deptno  
                                                                from paymas 
                                                                where facno ='001' and perno = @perno";

        
        public static string selectWorkno = $@"select workno 
                                        from paymas
                                        where facno ='001' and perno = @perno";

        public static string selectWorknoAlt = $@"select workno from paywkno where deptno = @deptno";



        public static string InsertIntoPddat = $@"INSERT INTO [dbo].[pcddat] 
                                            ([facno],
                                            [cardno],
                                            [perno],
                                            [attdate],
                                            [atttime],
                                            [attcode],
                                            [attfunc],
                                            [pcdcode],
                                            [pcddate],
                                            [workno],
                                            [iocode],
                                            [datasource],
                                            [moduserno],
                                            [moddate],
                                            [paymark1],
                                            [paymark4],
                                            [paymark5],
                                            [deptno],
                                            [userno],
                                            [pcsfh001],
                                            [pcsfh002],
                                            [pcsfh003],
                                            [remark],
                                            [cnvsta],
                                            [cardiocode],
                                            [prjno],
                                            [gps],
                                            [flowyn],
                                            [bodytemp]) 
                                        VALUES
                                            (@facno,
                                             @cardno,
                                             @perno,
                                             @attdate,
                                             @atttime,
                                             @attcode, 
                                             @attfunc,
                                             @pcdcode, 
                                             @pcddate, 
                                             @workno, 
                                             @iocode, 
                                             @datasource,
                                             @moduserno, 
                                             @moddate, 
                                             @paymark1,
                                             @paymark4, 
                                             @paymark5, 
                                             @deptno, 
                                             @userno, 
                                             @pcsfh001, 
                                             @pcsfh002, 
                                             @pcsfh003, 
                                             @remark, 
                                             @cnvsta, 
                                             @cardiocode, 
                                             @prjno, 
                                             @gps, 
                                             @flowyn, 
                                             @bodytemp)";


        public static string InsertIntoClockFile = $@"INSERT INTO [dbo].[auto_ClockFile]
                                                ([card_no],
                                                [staff_no],
                                                [clock_date],
                                                [clock_time],
                                                [clock_type],
                                                [work_no],
                                                [dept_no],
                                                [file_path])
                                            VALUES
                                                (@card_no,
                                                 @staff_no, 
                                                 @clock_date, 
                                                 @clock_time, 
                                                 @clock_type,
                                                 @work_no,
                                                 @dept_no,
                                                 @file_path)";

        public static string InsertIntoClockFileErrorLog = $@"INSERT INTO [dbo].[auto_ClockFileErrorLog]
                                                        ([file_path],
                                                        [error],
                                                        [card_no],
                                                        [clock_date],
                                                        [clock_time])
                                                    VALUES
                                                        (@file_path, 
                                                         @error,
                                                         @card_no,
                                                         @clock_date,
                                                         @clock_time)";


    }
}
