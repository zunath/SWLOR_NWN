import React from 'react';
import { Route, Switch, Redirect } from 'react-router';
import { Layout } from './components/Layout';
import Home from './components/Home';
import About from './components/About';
import Setting from './components/Setting';
import UserProfile from './components/UserProfile';
import Logs from './components/admin/logs/Logs';
import ChatLogs from './components/admin/logs/ChatLogs';
import ConnectionLogs from './components/admin/logs/ConnectionLogs';
import Admin from './components/Admin';
import NotFound from './components/NotFound';
import CharacterBackgrounds from './components/CharacterBackgrounds';
import Perks from './components/Perks';
import Skills from './components/Skills';
import FAQ from './components/FAQ';
import Species from './components/Species';
import Crafting from './components/Crafting';
import GameplayInfo from './components/GameplayInfo';
import Rules from './components/Rules';
import Downloads from './components/Downloads';
import Gallery from './components/Gallery';

export default () => (
    <Layout>
        <Switch>
            <Route exact path="/" component={Home} />
            <Route exact path="/home" component={Home} />
            <Route exact path="/about" component={About} />
            <Route exact path="/setting" component={Setting} />
            <Route exact path="/admin" component={Admin} />
            <Route exact path="/backgrounds" component={CharacterBackgrounds} />
            <Route exact path="/perks" component={Perks} />
            <Route exact path="/skills" component={Skills} />
            <Route exact path="/faq" component={FAQ} />
            <Route exact path="/setting" component={Setting} />
            <Route exact path="/species" component={Species} />
            <Route exact path="/crafting" component={Crafting} />
            <Route exact path="/gameplay-info" component={GameplayInfo} />
            <Route exact path="/rules" component={Rules} />
            <Route exact path="/downloads" component={Downloads} />
            <Route exact path="/gallery" component={Gallery} />

            <Route exact path="/user-profile" component={UserProfile} />

            <Route exact path="/admin/logs" component={Logs} />
            <Route exact path="/admin/chat-logs" component={ChatLogs} />
            <Route exact path="/admin/connection-logs" component={ConnectionLogs} />
            <Route path="/Download/Index/" />

            <Route exact path="/not-found" component={NotFound} />
            <Redirect from='*' to='/not-found' />
        </Switch>
    </Layout>
);
