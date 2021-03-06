using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

using DSharpPlus.Exceptions;

using Newtonsoft.Json;

using Npgsql;

using PluralKit.Core;

namespace PluralKit.Bot
{
    public static class MiscUtils {
        public static string ProxyTagsString(this PKMember member, string separator = ", ") => 
            string.Join(separator, member.ProxyTags.Select(t => t.ProxyString.AsCode()));
        
        
        private static String entityTerm<T>(int count, bool isTarget)
        {
            var ret = "";
            ret += isTarget ? "Member" : "Group";
            if ((
                (typeof(T) == typeof(GroupId) && !isTarget) ||
                (typeof(T) == typeof(MemberId) && isTarget)
            ) && count > 1)
                ret += "s";
            return ret;
        }

        public static String GroupAddRemoveResponse<T>(List<T> entityList, List<T> actionedOn, Groups.AddRemoveOperation op)
        {
            var opStr = op == Groups.AddRemoveOperation.Add ? "added to" : "removed from";
            var inStr = op == Groups.AddRemoveOperation.Add ? "in" : "not in";
            var notActionedOn = entityList.Count - actionedOn.Count;

            var groupNotActionedPosStr = typeof(T) == typeof(GroupId) ? notActionedOn.ToString() + " " : "";
            var memberNotActionedPosStr = typeof(T) == typeof(MemberId) ? notActionedOn.ToString() + " " : "";
            
            if (actionedOn.Count == 0)
                return $"{Emojis.Error} {entityTerm<T>(notActionedOn, true)} not {opStr} {entityTerm<T>(entityList.Count, false).ToLower()} ({entityTerm<T>(notActionedOn, true).ToLower()} already {inStr} {entityTerm<T>(entityList.Count, false).ToLower()}).";
            else
                if (notActionedOn == 0)
                    return $"{Emojis.Success} {entityTerm<T>(actionedOn.Count, true)} {opStr} {entityTerm<T>(actionedOn.Count, false).ToLower()}.";
                else
                    return $"{Emojis.Success} {entityTerm<T>(actionedOn.Count, true)} {opStr} {actionedOn.Count} {entityTerm<T>(actionedOn.Count, false).ToLower()} ({memberNotActionedPosStr}{entityTerm<T>(actionedOn.Count, true).ToLower()} already {inStr} {groupNotActionedPosStr}{entityTerm<T>(notActionedOn, false).ToLower()}).";
        }

        public static bool IsOurProblem(this Exception e)
        {
            // This function filters out sporadic errors out of our control from being reported to Sentry
            // otherwise we'd blow out our error reporting budget as soon as Discord takes a dump, or something.
            
            // Discord server errors are *not our problem*
            // TODO: DSharpPlus doesn't have a generic "HttpException" type and only special cases a couple response codes (that we don't need here)
            // Doesn't seem to handle 500s in the library at all, I'm not sure what it does in case it receives one...
            // if (e is DSharpPlus.Exceptions he && ((int) he.HttpCode) >= 500) return false;

            // Occasionally Discord's API will Have A Bad Time and return a bunch of CloudFlare errors (in HTML format).
            // The library tries to parse these HTML responses as JSON and crashes with a consistent exception message.
            if (e is JsonReaderException jre && jre.Message == "Unexpected character encountered while parsing value: <. Path '', line 0, position 0.") return false;

            // And now (2020-05-12), apparently Discord returns these weird responses occasionally. Also not our problem.
            if (e is BadRequestException bre && bre.WebResponse.Response.ToString().Contains("<center>nginx</center>")) return false;
            if (e is NotFoundException ne && ne.WebResponse.Response.ToString().Contains("<center>nginx</center>")) return false;
            if (e is UnauthorizedException ue && ue.WebResponse.Response.ToString().Contains("<center>nginx</center>")) return false;

            // 500s? also not our problem :^)
            if (e is ServerErrorException) return false;

            // Webhook server errors are also *not our problem*
            // (this includes rate limit errors, WebhookRateLimited is a subclass)
            if (e is WebhookExecutionErrorOnDiscordsEnd) return false;
            
            // Socket errors are *not our problem*
            if (e.GetBaseException() is SocketException) return false;
            
            // Tasks being cancelled for whatver reason are, you guessed it, also not our problem.
            if (e is TaskCanceledException) return false;

            // Sometimes Discord just times everything out.
            if (e is TimeoutException) return false;
            
            // Ignore "Database is shutting down" error
            if (e is PostgresException pe && pe.SqlState == "57P03") return false;
            
            // Ignore thread pool exhaustion errors
            if (e is NpgsqlException npe && npe.Message.Contains("The connection pool has been exhausted")) return false;

            // This may expanded at some point.
            return true;
        }
    }
}