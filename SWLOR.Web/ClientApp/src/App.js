import React from 'react';
import { Route, Switch, Redirect } from 'react-router';
import { Layout } from './components/Layout';
import Home from './components/Home';
import About from './components/About';
import NotFound from './components/NotFound';
import Rules from './components/Rules';
import Downloads from './components/Downloads';
import Gallery from './components/Gallery';

export default () => (
    <Layout>
        <Switch>
            <Route exact path="/" component={Home} />
            <Route exact path="/home" component={Home} />
            <Route exact path="/about" component={About} />
            <Route exact path="/rules" component={Rules} />
            <Route exact path="/downloads" component={Downloads} />
            <Route exact path="/gallery" component={Gallery} />

            <Route path="/Download/Index/" />

            <Route exact path="/not-found" component={NotFound} />
            <Redirect from='*' to='/not-found' />
        </Switch>
    </Layout>
);
